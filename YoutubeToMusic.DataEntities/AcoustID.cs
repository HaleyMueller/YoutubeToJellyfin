using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMusic.DataEntities
{
    public class Artist
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Date
    {
        public int year { get; set; }
        public int? day { get; set; }
        public int? month { get; set; }
    }

    public class Recording
    {
        public List<Artist> artists { get; set; }
        public double duration { get; set; }
        public Guid id { get; set; }
        public List<Release> releases { get; set; }
        public string title { get; set; }
    }

    public class Release
    {
        public List<Artist> artists { get; set; }
        public string id { get; set; }
        public int medium_count { get; set; }
        public string title { get; set; }
        public int track_count { get; set; }
        public string country { get; set; }
        public Date date { get; set; }
        public List<Releaseevent> releaseevents { get; set; }
    }

    public class Releaseevent
    {
        public string country { get; set; }
        public Date date { get; set; }
    }

    public class Result
    {
        public string id { get; set; }
        public List<Recording> recordings { get; set; }
        public double score { get; set; }
    }

    public class FingerprintLookup
    {
        public List<Result> results { get; set; }
        public string status { get; set; }
    }
}
