using System.Configuration;
using System.Data.SqlClient;
using Dota2ScorigamiDataPull.Entities;


namespace Dota2ScorigamiDataPull.Services
{
    public static class SQL
    {
        public static bool MatchExists(long matchId)
        {
            long result = 0;
            string query = "SELECT COUNT(*) FROM dbo.Matches WHERE match_id = " + matchId;

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Dota2Scorigami"].ToString()))
            {
                var command = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result = reader.GetInt32(0);
                    }
                }

                reader.Close();
            }

            return result != 0;
        }

        public static void AddMatch(Match match)
        {
            string query = "INSERT INTO [Dota2Scorigami].[dbo].[Matches] VALUES (";
            query += match.match_id.ToString() + ",";
            query += FormatNullableInt(match.start_time) + ",";
            query += FormatNullableInt(match.dire_score) + ",";
            query += FormatNullableInt(match.radiant_score) + ",";
            query += FormatNullableInt(match.league_id) + ",";
            query += (string.IsNullOrEmpty(match.league_name) ? "''" : "N'" + match.league_name + "'") + ",";
            query += FormatNullableInt(match.dire_team_id) + ",";
            query += (string.IsNullOrEmpty(match.dire_team_name) ? "''" : "N'" + match.dire_team_name + "'") + ",";
            query += FormatNullableInt(match.radiant_team_id) + ",";
            query += (string.IsNullOrEmpty(match.radiant_team_name) ? "''" : "N'" + match.radiant_team_name + "'") + ")";

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Dota2Scorigami"].ToString()))
            {
                conn.Open();
                var command = new SqlCommand(query, conn);
                command.ExecuteNonQuery();
            }
        }

        public static string GetTeamNameFromId(int teamId)
        {
            string result = null;

            string query = "SELECT name FROM dbo.Teams WHERE team_id = " + teamId;

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Dota2Scorigami"].ToString()))
            {
                var command = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result = reader.GetString(0);
                    }
                }

                reader.Close();
            }

            return result;
        }

        public static void AddTeam(Team team)
        {
            string query = "INSERT INTO [Dota2Scorigami].[dbo].[Teams] VALUES (";
            query += team.team_id.ToString() + ",";
            query += (string.IsNullOrEmpty(team.name) ? "''" : "N'" + team.name + "'") + ")";

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Dota2Scorigami"].ToString()))
            {
                conn.Open();
                var command = new SqlCommand(query, conn);
                command.ExecuteNonQuery();
            }
        }

        public static string FormatNullableInt(int? intValue)
        {
            return intValue == null ? "NULL" : intValue.ToString();
        }

        public static string FormatNullableBool(bool? boolValue)
        {
            if (boolValue == null)
            {
                return "NULL";
            }

            return (bool)boolValue ? "1" : "0";
        }
    }
}
