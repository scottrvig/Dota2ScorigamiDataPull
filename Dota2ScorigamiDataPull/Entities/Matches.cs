using System.Collections.Generic;


namespace Dota2ScorigamiDataPull.Entities
{
    public class Match
    {
        public long match_id { get; set; }
        public int? start_time { get; set; }
        public int? dire_score { get; set; }
        public int? radiant_score { get; set; }
        public int? leagueid { get; set; }
        public int? league_id { get; set; }
        public string league_name { get; set; }
        public int? dire_team_id { get; set; }
        public string dire_team_name { get; set; }
        public int? radiant_team_id { get; set; }
        public string radiant_team_name { get; set; }
        public List<Player> players { get; set; }
    }
}
