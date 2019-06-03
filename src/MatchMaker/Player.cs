using System;
using Newtonsoft.Json;

namespace MatchMaker
{
    public class Player
    {
        [JsonProperty(PropertyName = "isInGame")]
        public bool IsInGame { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public bool IsReadyForGame { get; set; }
        public DateTime LastSeen { get; set; }
    }
}