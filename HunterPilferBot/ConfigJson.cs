using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HunterPilferBot
{
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set }
        [JsonProperty("Prefix")]
        public string Prefix { get; private set }

    }
}
