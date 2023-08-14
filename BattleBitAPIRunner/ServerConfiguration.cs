using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ServerConfiguration
    {
        [Required]
        public string? IP { get; set; }

        [Required]
        public int? Port { get; set; }

        public IPAddress? IPAddress { get; set; }

        public string ModulePath { get; set; } = "./modules";

        public int ModuleScanInterval { get; set; } = 1000;

        public string[] Modules { get; set; } = Array.Empty<string>();
    }
}
