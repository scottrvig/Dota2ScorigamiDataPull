using System;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

using Flurl;

using Dota2ScorigamiDataPull.Entities;
using Dota2ScorigamiDataPull.Services;


namespace Dota2ScorigamiDataPull
{
    class Program
    {
        static void Main(string[] args)
        {
            RunScript().Wait();
        }

        static async Task RunScript()
        {
            var start_time = DateTime.Now;

            Utility.InitializeComponents();
            Utility.LogInfo("".PadRight(60, '='), true);
            Utility.LogInfo("Start time: " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), true);
            Utility.LogInfo("".PadRight(60, '='), true);

            try
            {
                var leagues = await GetLeagues();

                var leaguesToQuery = leagues.Where(l => l.name.ToLower().Contains("the international") ||
                                                        l.name.ToLower().Contains(" major") ||
                                                        l.name.ToLower().Contains(" asia championship") ||
                                                        l.name.ToLower().Contains("dpc") ||
                                                        l.name.ToLower().Contains("dota pro circuit")).ToList();

                // Exclusions/one offs/qualifiers
                leaguesToQuery = leaguesToQuery.Where(l => !l.name.ToLower().Contains("qualifier") &&
                                                           !l.name.ToLower().Contains("bautumn major") &&
                                                           !l.name.ToLower().Contains("palembang") &&
                                                           !l.name.ToLower().Contains("dota circle") &&
                                                           !l.name.ToLower().Contains("prime major tournament")).ToList();

                int i = 1;
                foreach (var l in leaguesToQuery)
                {
                    Utility.LogInfo("League " + i + " of " + leaguesToQuery.Count() + ": " + l.name);
                    var leagueMatches = await GetLeagueMatches(l);
                    foreach (var m in leagueMatches)
                    {
                        // Match doesn't have a valid score or team, skip junk data
                        if (!m.radiant_score.HasValue || !m.dire_score.HasValue || !m.radiant_team_id.HasValue || !m.dire_team_id.HasValue) continue;

                        // Match already exists, skip it
                        if (SQL.MatchExists(m.match_id)) continue;

                        if (m.radiant_score.Value == 0 && m.dire_score.Value == 0)
                        {
                            // Special case. For some reason OpenDota doesn't have the kill total when querying matches through the League list for older games
                            // Workaround is to query the match directly and sum up kills recorded on the player objects.
                            var matchFullDetails = await GetMatch(m.match_id);

                            m.radiant_score = matchFullDetails.players.Where(p => p.isRadiant).Select(p => p.kills).Sum();
                            m.dire_score = matchFullDetails.players.Where(p => !p.isRadiant).Select(p => p.kills).Sum();
                        }

                        // If team scores are still 0, it's really a bad game (e.g. match id 6644286608)
                        if (m.radiant_score.Value == 0 && m.dire_score.Value == 0) continue;

                        Utility.LogInfo("Adding match " + m.match_id);

                        m.league_id = m.leagueid;
                        m.league_name = l.name;
                        m.radiant_team_name = await GetTeamNameFromId(m.radiant_team_id.Value);
                        m.dire_team_name = await GetTeamNameFromId(m.dire_team_id.Value);

                        // Add the match to the DB
                        SQL.AddMatch(m);
                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                Utility.LogError(ex.Message);
                if (ex.InnerException != null)
                {
                    Utility.LogError(ex.InnerException.Message);
                }
            }

            TimeSpan ts = DateTime.Now - start_time;
            int hourDiff = ts.Hours;
            int minDiff = ts.Minutes;
            int secDiff = ts.Seconds;

            Utility.LogInfo("Dota2ScorigamiDataPull program completed in " + hourDiff.ToString().PadLeft(2, '0') + ":" + minDiff.ToString().PadLeft(2, '0') + ":" + secDiff.ToString().PadLeft(2, '0'), true);
            Console.ReadLine();
        }

        private static async Task<string> GetTeamNameFromId(int teamId)
        {
            string teamName = SQL.GetTeamNameFromId(teamId);

            if (teamName == null)
            {
                // Look up from OpenDota
                teamName = await GetTeam(teamId);

                // Watch for single quotes as they break SQL
                teamName = teamName.Replace("'", "").Replace("\"", "").Trim();

                // Add team to database
                SQL.AddTeam(new Team()
                {
                    team_id = teamId,
                    name = teamName
                });
            }

            return teamName;
        }

        static async Task<List<League>> GetLeagues()
        {
            string url = Url.Combine(ConfigurationManager.AppSettings["OpenDotaBaseUrl"], "leagues");

            var result = await REST.ExecuteAsnycGet<List<League>>(url);
            return result;
        }

        static async Task<List<Match>> GetLeagueMatches(League league)
        {
            string url = Url.Combine(ConfigurationManager.AppSettings["OpenDotaBaseUrl"], "leagues", league.leagueid.ToString(), "matches");

            var result = await REST.ExecuteAsnycGet<List<Match>>(url);
            return result;
        }

        static async Task<Match> GetMatch(long matchId)
        {
            string url = Url.Combine(ConfigurationManager.AppSettings["OpenDotaBaseUrl"], "matches", matchId.ToString());

            var result = await REST.ExecuteAsnycGet<Match>(url);
            return result;
        }

        static async Task<string> GetTeam(int teamId) {
            string url = Url.Combine(ConfigurationManager.AppSettings["OpenDotaBaseUrl"], "teams", teamId.ToString());

            var result = await REST.ExecuteAsnycGet<Team>(url);
            return result.name ?? "";
        }
    }
}
