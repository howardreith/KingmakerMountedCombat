// Compiles the observer's actual request DTO and validator and reads each JSON
// file named on the command line exactly as the observer process would.
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace KmcRemovalObserver
{
    public static class RequestProbe
    {
        public static int Main(string[] files)
        {
            var settings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error };
            foreach (var file in files)
            {
                string verdict;
                try
                {
                    var request = JsonConvert.DeserializeObject<ObserverRequest>(new UTF8Encoding(false, true).GetString(File.ReadAllBytes(file)), settings);
                    var errors = request.Validate();
                    verdict = errors.Count == 0 ? "VALID normalPlayer=" + request.IsNormalPlayer : "INVALID " + string.Join(" | ", errors);
                }
                catch (Exception exception) { verdict = "REFUSED " + exception.GetType().Name; }
                Console.WriteLine(Path.GetFileName(file) + " " + verdict);
            }
            return 0;
        }
    }
}
