using System;
using System.Collections;
using System.IO;
using System.Runtime.Versioning;
//using Meow.AssetUpdater.Core;
using LitJson;
using UnityEngine;
using AssetUX;

namespace AssetUX
{
    public class MainUpdater : MonoBehaviour
    {
        private VersionInfo _remoteVersionInfo;
        [SerializeField]
        private VersionInfo _persistentVersionInfo;
        private VersionInfo _streamingVersionInfo;

        public string RemoteUrl;
        public string ProjectName;
        public string VersionFileName;

#if UNITY_EDITOR
        private static int _isSimulationMode = -1;

        public static bool IsSimulationMode
        {
            get
            {
                if (_isSimulationMode == -1)
                    _isSimulationMode = UnityEditor.EditorPrefs.GetBool("AssetUpdaterSimulationMode", true) ? 1 : 0;

                return _isSimulationMode != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _isSimulationMode)
                {
                    _isSimulationMode = newValue;
                    UnityEditor.EditorPrefs.SetBool("AssetUpdaterSimulationMode", value);
                }
            }
        }
#endif

        private void Awake()
        {
            /*RemoteUrl = Settings.RemoteUrl; //ԭ��� Edit 2017.8.21
            ProjectName = Settings.RelativePath;
            VersionFileName = Settings.VersionFileName;*/
        }

        /// <summary>
        /// Initialize global settings of AssetUpdater
        /// </summary>
        /// <param name="remoteUrl">the assetbundle server url you want to download from</param>
        /// <param name="projectName">the relative directory of your assetbundle root path</param>
        /// <param name="versionFileName">name of version file, default by "versionfile.bytes"</param>
        public void Initialize(string remoteUrl, string projectName, string versionFileName)
        {
            RemoteUrl = remoteUrl;
            ProjectName = projectName;
            VersionFileName = versionFileName;
        }

        /// <summary>
        /// download all version files which in remote url, persistentDataPath and streamingAssetPath. perpare for downloading
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadAllVersionFiles()
        {
#if UNITY_EDITOR
            if (!IsSimulationMode)
#endif
            {
                //var vFRoot = Path.Combine(ProjectName, Utils.GetBuildPlatform(Application.platform).ToString());//ԭ��� 
                //var vFPath = Path.Combine(vFRoot, VersionFileName);                                             //ԭ���                

                var vFRoot = ProjectName + "/" + Application.platform.ToString();
                var vFPath = vFRoot + "/" + VersionFileName; 

                var op = new DownloadOperation(this, SourceType.PersistentPath, vFPath);//�ȴ�PersistentPath����汾�ļ�
                yield return op;
                _persistentVersionInfo = JsonMapper.ToObject<VersionInfo>(op.Text);
                if (_persistentVersionInfo == null)
                {
                    _persistentVersionInfo = new VersionInfo();
                }
                else  //PersistentPath ·���İ汾�ļ�Ϊ��;
                {
                    op = new DownloadOperation(this, SourceType.StreamingPath, vFPath);//��ȡStreamingPath��İ汾�ļ�
                    yield return op;
                    _streamingVersionInfo = JsonMapper.ToObject<VersionInfo>(op.Text);
                    if (_streamingVersionInfo == null)
                    {
                        _streamingVersionInfo = new VersionInfo();
                    }
                    else //û�а汾�ļ����ж�;
                    {
                        Debug.LogError("streamingPath��û�а汾�ļ�");
                    }

                }



                Debug.Log("LoadAllVersionFiles vPath = " + vFPath);// Edit wxw 2017.8.15

                op = new DownloadOperation(this, SourceType.RemotePath, vFPath);
                yield return op;
                if (!string.IsNullOrEmpty(op.Error))
                {
                    Debug.LogError("Can not download remote version file, error = " + op.Error);
                    yield break;
                }
                _remoteVersionInfo = JsonMapper.ToObject<VersionInfo>(op.Text);

                
            }
        }

        /// <summary>
        /// download files from streamingAssetPath to persistentDataPath, then update versionfile in persistentDataPath
        /// </summary>
        /// <returns>updateOperation object contains current downloading information</returns>
        public UpdateOperation UpdateFromStreamingAsset()
        {
            return new UpdateOperation(this, _persistentVersionInfo, _streamingVersionInfo, SourceType.StreamingPath);
        }

        public UpdateOperation UpdateFromRemoteAsset()
        {
            return new UpdateOperation(this, _persistentVersionInfo, _remoteVersionInfo, SourceType.RemotePath);
        }

        /// <summary>
        /// get assetbundle name by input asset path
        /// </summary>
        /// <param name="path">asset path</param>
        /// <returns>assetbundle name</returns>
        public string GetAssetbundlePathByAssetPath(string path)
        {
            string result = "";
#if UNITY_EDITOR
            if (!IsSimulationMode)
#endif
            {
                if (_persistentVersionInfo.BundlePath.ContainsKey(path.ToLower()))
                {
                    result = _persistentVersionInfo.BundlePath[path.ToLower()];
                }
                else
                {
                    Debug.LogErrorFormat("Given asset [{0}] path is not exist in any downloaded assetbundles", path);
                }
            }
            return result;
        }

        public string GetAssetbundleRootPath(bool forWWW)
        {
            var path = Path.Combine(Path.Combine(Application.persistentDataPath, ProjectName), Utils.GetBuildPlatform().ToString());

            if (forWWW)
            {
                path = "file://" + path;
            }
            return path;
        }

        public string GetManifestName()
        {
            return Utils.GetBuildPlatform().ToString();
        }
    }
}