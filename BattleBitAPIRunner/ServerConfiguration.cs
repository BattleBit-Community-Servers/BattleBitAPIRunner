using BattleBitAPI.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ServerConfiguration
    {
        [Required]
        public string? IP { get; set; } = "127.0.0.1";

        [Required]
        public int? Port { get; set; } = 29294;

        [JsonIgnore]
        public IPAddress? IPAddress { get; set; }

        public string ModulesPath { get; set; } = "./modules";

        public string[] Modules { get; set; } = Array.Empty<string>();

        public string DependencyPath { get; set; } = "./dependencies";
        public string ConfigurationPath { get; set; } = "./configurations";
        public LogLevel LogLevel { get; set; } = LogLevel.None;
        public int WarningThreshold { get; set; } = 250;
    }
}
