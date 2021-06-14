using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FigmaImporter.Editor.EditorTree;
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
        private static Canvas _canvas;
        private static List<Node> _nodes = null;
        private MultiColumnLayout treeView;
        private string _lastClickedNode = String.Empty;
        Dictionary<string, Texture2D> _texturesCache = new Dictionary<string, Texture2D>();

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
            if (GUILayout.Button("Get Node Data"))
            {
                string apiUrl = ConvertToApiUrl(_settings.Url);
                //GetFile(apiUrl);
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
            treeView.TreeView.OnItemClick -= ItemClicked;
        }

        private void DrawNodeTree()
        {
            bool justCreated = false;
            if (treeView == null)
            {
                treeView = new MultiColumnLayout();
                justCreated = true;
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            var width = position.width / 2f;
            var treeRect = new Rect(0, lastRect.yMax + 20, width, this.position.height - lastRect.yMax - 50);
            treeView.OnGUI(treeRect, _nodes);
            if (justCreated)
                treeView.TreeView.OnItemClick += ItemClicked;
        }

        private async void ItemClicked(string obj)
        {
            Debug.Log($"[FigmaImporter] {obj} clicked");
            _lastClickedNode = obj;
            if (!_texturesCache.TryGetValue(obj, out var tex))
            {
                _texturesCache[obj] = await GetImage(obj, false);
            }

            Repaint();
        }

        private void ShowExecuteButton()
        {
        }

        public async Task GetNodes(string url)
        {
            _nodes = await GetNodeInfo(url);
        }

        private static string _fileName;
        private static string _nodeId;

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
            FigmaNodesProgressInfo.CurrentNode = FigmaNodesProgressInfo.NodesCount = 0;
            FigmaNodesProgressInfo.CurrentTitle = "Loading nodes info";
            var nodes = await GetNodeInfo(fileUrl);
            FigmaNodeGenerator generator = new FigmaNodeGenerator(this);
            foreach (var node in nodes)
                await generator.GenerateNode(node);
            FigmaNodesProgressInfo.HideProgress();
        }

        private async Task<List<Node>> GetNodeInfo(string nodeUrl)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(nodeUrl))
            {
                www.SetRequestHeader("Authorization", $"Bearer {_settings.Token}");
                www.SendWebRequest();
                while (!www.isDone)
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

        private const string ImagesUrl = "https://api.figma.com/v1/images/{0}?ids={1}&svg_include_id=true&format=png";

        public async Task<Texture2D> GetImage(string nodeId, bool showProgress = true)
        {
            WWWForm form = new WWWForm();
            string request = string.Format(ImagesUrl, _fileName, nodeId);
            using (UnityWebRequest www = UnityWebRequest.Get(request))
            {
                www.SetRequestHeader("Authorization", $"Bearer {_settings.Token}");
                www.SendWebRequest();
                while (!www.isDone)
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
                            return await LoadTextureByUrl(s, showProgress);
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