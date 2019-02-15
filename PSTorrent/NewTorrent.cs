using MonoTorrent.BEncoding;
using MonoTorrent.Common;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace PSTorrent
{
    [Cmdlet(VerbsCommon.New, "Torrent")]
    [Alias("mktorrent")]
    public class NewTorrent : PSCmdlet
    {
        #region Private Objects
        /// <summary>
        /// 路径是否为文件夹
        /// </summary>
        private bool isDirectory;

        /// <summary>
        /// 种子创建器
        /// </summary>
        private TorrentCreator torrentCreator = new TorrentCreator();

        /// <summary>
        /// 当前命令行指示路径
        /// </summary>
        private string currentLocation;

        #endregion

        #region Command Parameters
        /// <summary>
        /// Tracker服务器地址
        /// </summary>
        [Parameter]
        [Alias("a")]
        [AllowEmptyCollection]
        public string[] AnnounceUrls { get; set; }

        /// <summary>
        /// 注释
        /// </summary>
        [Parameter]
        [Alias("c")]
        [AllowNull]
        public string Comment { get; set; }

        /// <summary>
        /// 制作软件
        /// </summary>
        [Parameter]
        [AllowNull]
        public string CreatedBy { get; set; } = "PSTorrent v1.0";

        /// <summary>
        /// 文件或文件夹位置
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string FilePath { get; set; }

        /// <summary>
        /// 忽略隐藏文件
        /// </summary>
        [Parameter]
        public SwitchParameter IgnoreHidden { get; set; } = true;

        /// <summary>
        /// 分块长度
        /// </summary>
        [Parameter]
        [Alias("l")]
        [AllowNull]
        public int? PieceLength { get; set; }

        /// <summary>
        /// 私有Tracker服务器
        /// </summary>
        [Parameter]
        [Alias("p")]
        public SwitchParameter Private { get; set; } = false;

        /// <summary>
        /// 种子保存路径
        /// </summary>
        [Parameter]
        [AllowEmptyString]
        public string SavePath { get; set; }
        

        /// <summary>
        /// 源网址
        /// </summary>
        [Parameter]
        [AllowEmptyString]
        public string Source { get; set; }

        /// <summary>
        /// Web种子链接
        /// </summary>
        [Parameter]
        [Alias("w")]
        [AllowEmptyCollection]
        public string[] WebSeedLinks { get; set; }

        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void BeginProcessing()
        {
            currentLocation = CurrentProviderLocation("").ProviderPath;

            //验证文件是否为存在
            if (Directory.Exists(FilePath))
            {
                isDirectory = true;
            }
            else if (File.Exists(FilePath))
            {
                isDirectory = false;
            }
            else if (Directory.Exists(currentLocation + "\\" + FilePath))
            {
                isDirectory = true;
                FilePath = currentLocation + "\\" + FilePath;
            }
            else if (File.Exists(currentLocation + "\\" + FilePath))
            {
                isDirectory = false;
                FilePath = currentLocation + "\\" + FilePath;
            }
            else
            {
                WriteError(new ErrorRecord(new DirectoryNotFoundException(), "", ErrorCategory.ObjectNotFound, FilePath));
                return;
            }

            //分块长度为空或0时计算长度
            if (PieceLength == null || PieceLength <= 0)
                if (isDirectory)
                    torrentCreator.PieceLength = TorrentCreator.RecommendedPieceSize(Directory.GetFiles(FilePath, "*", SearchOption.AllDirectories));
                else
                    torrentCreator.PieceLength = TorrentCreator.RecommendedPieceSize(new string[] { FilePath });
            else
                torrentCreator.PieceLength = (long)PieceLength;

            //元数据添加
            torrentCreator.Private = Private;
            if (Comment != null)
            {
                torrentCreator.Comment = Comment;
            }
            torrentCreator.CreatedBy = CreatedBy;
            if (Source != null)
            {
                torrentCreator.SetCustomSecure("source", new BEncodedString(Source));
            }
            if (WebSeedLinks != null)
            {
                foreach (var link in WebSeedLinks)
                {
                    torrentCreator.GetrightHttpSeeds.Add(link);
                }
            }
            if (AnnounceUrls != null)
            {
                torrentCreator.Announce = AnnounceUrls[0];
                if (AnnounceUrls.Length > 1)
                {
                    torrentCreator.Announces.Add(new MonoTorrent.RawTrackerTier(AnnounceUrls));
                }
            }

            //保存路径修正
            if (SavePath == null)
            {
                if (isDirectory)
                {
                    SavePath = Directory.GetParent(FilePath).FullName + "\\" + new DirectoryInfo(FilePath).Name + ".torrent";
                }
                else
                {
                    SavePath = new FileInfo(FilePath).Directory.FullName + "\\" + new FileInfo(FilePath).Name + ".torrent";
                }
            }
            else if (!Path.IsPathRooted(SavePath))
            {
                if (isDirectory)
                {
                    SavePath = Directory.GetParent(FilePath).FullName + "\\" + SavePath;
                }
                else
                {
                    SavePath = new FileInfo(FilePath).Directory.FullName + "\\" + SavePath;
                }
            }
        }

        protected override void ProcessRecord()
        {
            try
            {
                torrentCreator.Create(new TorrentFileSource(FilePath, IgnoreHidden), SavePath);
            }
            catch (TorrentException e)
            {
                WriteError(new ErrorRecord(e, "", ErrorCategory.InvalidOperation, ""));
            }
            catch (IOException e)
            {
                WriteError(new ErrorRecord(e, "", ErrorCategory.WriteError, ""));
            }
        }
    }
}