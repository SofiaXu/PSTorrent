using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSTorrent
{
    [RunInstaller(true)]
    public class PSTorrentSnapIn : PSSnapIn
    {
        public override string Name => "PSTorrent";

        public override string Vendor => "Aoba";

        public override string Description => "A simple torrent editer for PowerShell powered by monoTorrent.";

        public PSTorrentSnapIn() : base()
        {

        }
    }
}
