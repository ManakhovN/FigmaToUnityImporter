using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        void OnGUI()
        {
            if (_settings == null)
                _settings = FigmaImporterSettings.GetInstance();

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
            if (GUILayout.Button("GetFile"))
            {
                string apiUrl = ConvertToApiUrl(_settings.Url);
                GetFile(apiUrl);
            }
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
            WWWForm form = new WWWForm();
            string request = fileUrl;
            FigmaNodesProgressInfo.CurrentNode = FigmaNodesProgressInfo.NodesCount = 0;
            FigmaNodesProgressInfo.CurrentTitle = "Loading nodes info";
            using (UnityWebRequest www = UnityWebRequest.Get(request))
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
                    var nodes = parser.ParseResult(result);
                    FigmaNodesProgressInfo.NodesCount = GetNodesCount(nodes);
                    FigmaNodeGenerator generator = new FigmaNodeGenerator(this);
                    foreach (var node in nodes)
                        await generator.GenerateNode(node);
                }
                FigmaNodesProgressInfo.HideProgress();
            }
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

        public async Task<Texture2D> GetImage(string nodeId)
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
                            return await LoadTextureByUrl(s);
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

        private async Task<Texture2D> LoadTextureByUrl(string url)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.SendWebRequest();
                while (request.downloadProgress<1f)
                {
                    FigmaNodesProgressInfo.ShowProgress(request.downloadProgress);
                    await Task.Delay(100);
                }
                if (request.isNetworkError || request.isHttpError)
                    return null;
                var data = request.downloadHandler.data;
                Texture2D t = new Texture2D(0,0);
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