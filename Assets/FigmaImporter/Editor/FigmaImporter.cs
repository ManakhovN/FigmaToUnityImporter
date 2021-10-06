using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FigmaImporter.Editor.EditorTree;
using FigmaImporter.Editor.EditorTree.TreeData;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace FigmaImporter.Editor
{
    public class FigmaImporter : EditorWindow
    {
        [MenuItem("Window/FigmaImporter")]
        static void Init()
        {
            FigmaImporter window = (FigmaImporter) EditorWindow.GetWindow(typeof(FigmaImporter));
            window.Show();
        }

        private static FigmaImporterSettings _settings = null;
        private static GameObject _rootObject;
        private static List<Node> _nodes = null;
        private MultiColumnLayout _treeView;
        private string _lastClickedNode = String.Empty;
        
        private static string _fileName;
        private static string _nodeId;
        private float _scale = 1f;

        Dictionary<string, Texture2D> _texturesCache = new Dictionary<string, Texture2D>();

        public float Scale => _scale;

        void OnGUI()
        {
            if (_settings == null)
                _settings = FigmaImporterSettings.GetInstance();

            int currentPosY = 0;
            if (GUILayout.Button("OpenOauthUrl"))
            {
                OpenOauthUrl();
            }

            _settings.ClientCode = EditorGUILayout.TextField("ClientCode", _settings.ClientCode);
            _settings.State = EditorGUILayout.TextField("State", _settings.State);
            
            EditorUtility.SetDirty(_settings);

            if (GUILayout.Button("GetToken"))
            {
                _settings.Token = GetOAuthToken();
            }

            GUILayout.TextArea("Token:" + _settings.Token);
            _settings.Url = EditorGUILayout.TextField("Url", _settings.Url);
            _settings.RendersPath = EditorGUILayout.TextField("RendersPath", _settings.RendersPath);

            _rootObject =
                (GameObject) EditorGUILayout.ObjectField("Root Object", _rootObject, typeof(GameObject), true);
            
            _scale = EditorGUILayout.Slider("Scale", _scale, 0.01f, 4f);
            
            var redStyle = new GUIStyle(EditorStyles.label);

            redStyle.normal.textColor = UnityEngine.Color.red;
            EditorGUILayout.LabelField(
                "Preview on the right side loaded via Figma API. It doesn't represent the final result!!!!", redStyle);

            if (GUILayout.Button("Get Node Data"))
            {
                string apiUrl = ConvertToApiUrl(_settings.Url);
                GetNodes(apiUrl);
            }

            if (_nodes != null)
            {
                DrawNodeTree();
                DrawPreview();
                ShowExecuteButton();
            }
        }

        private void DrawPreview()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            var widthMax = position.width / 2f;
            var heightMax = this.position.height - lastRect.yMax - 50;
            var height = heightMax;
            var width = widthMax;
            _texturesCache.TryGetValue(_lastClickedNode, out var lastLoadedPreview);
            if (lastLoadedPreview != null)
            {
                if (lastLoadedPreview.width < widthMax && lastLoadedPreview.height < heightMax)
                {
                    width = lastLoadedPreview.width;
                    height = lastLoadedPreview.height;
                }
                else
                {
                    height = widthMax * lastLoadedPreview.height / lastLoadedPreview.width;
                    if (height > heightMax)
                    {
                        height = heightMax;
                        width = heightMax * lastLoadedPreview.width / lastLoadedPreview.height;
                    }
                }
            }

            var previewRect = new Rect(position.width / 2f, lastRect.yMax + 20, width, height);
            if (lastLoadedPreview != null)
                GUI.DrawTexture(previewRect, lastLoadedPreview);
        }

        private void OnDestroy()
        {
            if (_treeView != null && _treeView.TreeView != null)
                _treeView.TreeView.OnItemClick -= ItemClicked;
            _treeView = null;
            _nodes = null;
            foreach (var texture in _texturesCache)
            {
                DestroyImmediate(texture.Value);
            }

            _texturesCache.Clear();
        }

        private void DrawNodeTree()
        {
            bool justCreated = false;
            if (_treeView == null)
            {
                _treeView = new MultiColumnLayout();
                justCreated = true;
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            var width = position.width / 2f;
            var treeRect = new Rect(0, lastRect.yMax + 20, width, this.position.height - lastRect.yMax - 50);
            _treeView.OnGUI(treeRect, _nodes);
            var nodesTreeElements = _treeView.TreeView.treeModel.Data;
            if (justCreated)
            {
                _treeView.TreeView.OnItemClick += ItemClicked;
                NodesAnalyzer.Analyze(_nodes, nodesTreeElements);
                LoadAllRenders(nodesTreeElements);
            }

            NodesAnalyzer.CheckActions(_nodes, nodesTreeElements);
        }

        private async void LoadAllRenders(IList<NodeTreeElement> nodesTreeElements)
        {
            if (nodesTreeElements == null || nodesTreeElements.Count == 0)
                return;
            await Task.WhenAll(nodesTreeElements.Select(x => GetImage(x.figmaId)));
            _lastClickedNode = nodesTreeElements[0].figmaId;
        }

        private async void ItemClicked(string obj)
        {
            Debug.Log($"[FigmaImporter] {obj} clicked");
            _lastClickedNode = obj;
            if (!_texturesCache.TryGetValue(obj, out var tex))
            {
                await GetImage(obj, false);
            }
            Repaint();
        }

        private void ShowExecuteButton()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            var buttonRect = new Rect(lastRect.xMin, this.position.height - 30, lastRect.width, 30f);
            if (GUI.Button(buttonRect, "Generate nodes"))
            {
                string apiUrl = ConvertToApiUrl(_settings.Url);
                GetFile(apiUrl);
            }
        }

        public async Task GetNodes(string url)
        {
            OnDestroy();
            _nodes = await GetNodeInfo(url);
            FigmaNodesProgressInfo.HideProgress();
        }

        private string ConvertToApiUrl(string s)
        {
            var substrings = s.Split('/');
            var length = substrings.Length;
            bool isNodeUrl = substrings[length - 1].Contains("node-id");
            _fileName = substrings[length - 2];
            if (!isNodeUrl)
            {
                return $"https://api.figma.com/v1/files/{_fileName}";
            }

            _nodeId = substrings[length - 1]
                .Split(new string[] {"?node-id="}, StringSplitOptions.RemoveEmptyEntries)[1];
            return $"https://api.figma.com/v1/files/{_fileName}/nodes?ids={_nodeId}";
        }

        private const string ApplicationKey = "msRpeIqxmc8a7a6U0Z4Jg6";
        private const string RedirectURI = "https://manakhovn.github.io/figmaImporter";

        private const string OAuthUrl =
            "https://www.figma.com/oauth?client_id={0}&redirect_uri={1}&scope=file_read&state={2}&response_type=code";

        public void OpenOauthUrl()
        {
            var state = Random.Range(0, Int32.MaxValue);
            string formattedOauthUrl = String.Format(OAuthUrl, ApplicationKey, RedirectURI, state.ToString());
            Application.OpenURL(formattedOauthUrl);
        }

        private const string ClientSecret = "VlyvMwuA4aVOm4dxcJgOvxbdWsmOJE";

        private const string AuthUrl =
            "https://www.figma.com/api/oauth/token?client_id={0}&client_secret={1}&redirect_uri={2}&code={3}&grant_type=authorization_code";

        private string GetOAuthToken()
        {
            WWWForm form = new WWWForm();
            string request = String.Format(AuthUrl, ApplicationKey, ClientSecret, RedirectURI, _settings.ClientCode);
            using (UnityWebRequest www = UnityWebRequest.Post(request, form))
            {
                www.SendWebRequest();

                while (!www.isDone)
                {
                }

                if (www.isNetworkError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    var result = www.downloadHandler.text;
                    Debug.Log(result);
                    return JsonUtility.FromJson<AuthResult>(result).access_token;
                }
            }

            return "";
        }

        private async void GetFile(string fileUrl)
        {
            if (_rootObject == null)
            {
                Debug.LogError($"[FigmaImporter] Root object is null. Please add reference to a Canvas or previous version of the object");
                return;
            }

            if (_nodes == null)
            {
                FigmaNodesProgressInfo.CurrentNode = FigmaNodesProgressInfo.NodesCount = 0;
                FigmaNodesProgressInfo.CurrentTitle = "Loading nodes info";
                await GetNodes(fileUrl);
            }

            FigmaNodeGenerator generator = new FigmaNodeGenerator(this);
            foreach (var node in _nodes)
            {
                var nodeTreeElements = _treeView.TreeView.treeModel.Data;
                await generator.GenerateNode(node, _rootObject, nodeTreeElements);
            }

            FigmaNodesProgressInfo.HideProgress();
        }

        private async Task<List<Node>> GetNodeInfo(string nodeUrl)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(nodeUrl))
            {
                www.SetRequestHeader("Authorization", $"Bearer {_settings.Token}");
                www.SendWebRequest();
                while (!www.isDone && !www.isNetworkError)
                {
                    FigmaNodesProgressInfo.CurrentInfo = "Loading nodes info";
                    FigmaNodesProgressInfo.ShowProgress(www.downloadProgress);
                    await Task.Delay(100);
                }

                FigmaNodesProgressInfo.HideProgress();

                if (www.isNetworkError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    var result = www.downloadHandler.text;
                    FigmaParser parser = new FigmaParser();
                    return parser.ParseResult(result);
                }

                FigmaNodesProgressInfo.HideProgress();
            }

            return null;
        }

        private int GetNodesCount(IEnumerable<Node> nodes)
        {
            int count = 0;
            if (nodes == null)
                return 0;
            foreach (var node in nodes)
            {
                count++;
                count += GetNodesCount(node.children);
            }

            return count;
        }

        private const string ImagesUrl = "https://api.figma.com/v1/images/{0}?ids={1}&svg_include_id=true&format=png&scale={2}";

        public async Task<Texture2D> GetImage(string nodeId, bool showProgress = true)
        {
            if (_texturesCache.TryGetValue(nodeId, out var tex))
            {
                return _texturesCache[nodeId];
            }

            WWWForm form = new WWWForm();
            string request = string.Format(ImagesUrl, _fileName, nodeId, _scale);
            using (UnityWebRequest www = UnityWebRequest.Get(request))
            {
                www.SetRequestHeader("Authorization", $"Bearer {_settings.Token}");
                www.SendWebRequest();
                while (!www.isDone && !www.isNetworkError)
                {
                    FigmaNodesProgressInfo.CurrentInfo = "Getting node image info";
                    if (showProgress)
                        FigmaNodesProgressInfo.ShowProgress(www.downloadProgress);
                    await Task.Delay(100);
                }

                FigmaNodesProgressInfo.HideProgress();

                if (www.isNetworkError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    var result = www.downloadHandler.text;
                    var substrs = result.Split('"');
                    FigmaNodesProgressInfo.CurrentInfo = "Loading node texture";
                    foreach (var s in substrs)
                    {
                        if (s.Contains("http"))
                        {
                            var texture = await LoadTextureByUrl(s, showProgress);
                            _texturesCache[nodeId] = texture;
                            return texture;
                        }
                    }
                }
            }

            return null;
        }

        public string GetRendersFolderPath()
        {
            return _settings.RendersPath;
        }

        private async Task<Texture2D> LoadTextureByUrl(string url, bool showProgress = true)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.SendWebRequest();
                while (request.downloadProgress < 1f)
                {
                    if (showProgress)
                        FigmaNodesProgressInfo.ShowProgress(request.downloadProgress);
                    await Task.Delay(100);
                }

                if (request.isNetworkError || request.isHttpError)
                    return null;
                var data = request.downloadHandler.data;
                Texture2D t = new Texture2D(0, 0);
                t.LoadImage(data);
                FigmaNodesProgressInfo.HideProgress();
                return t;
            }
        }


        [Serializable]
        public class AuthResult
        {
            [SerializeField] public string access_token;
            [SerializeField] public string expires_in;
            [SerializeField] public string refresh_token;
        }
    }
}