using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIV_Kafka
{
    public class AppSettings
    {
        public Nlog NLog { get; set; }
        public Connectionstrings ConnectionStrings { get; set; }
        public Kafkaconfig KafkaConfig { get; set; }

        public class Nlog
        {
            public bool autoReload { get; set; }
            public string internalLogLevel { get; set; }
            public string internalLogFile { get; set; }
            public Targets targets { get; set; }
            public Rule[] rules { get; set; }
        }

        public class Targets
        {
            public OwnfileLifetime ownFileLifetime { get; set; }
            public Allfile allfile { get; set; }
            public Error error { get; set; }
            public Debug Debug { get; set; }
            public Traceinfo Traceinfo { get; set; }
            public Info info { get; set; }
            public Dbfile DBFile { get; set; }
        }

        public class OwnfileLifetime
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Allfile
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Error
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Debug
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Traceinfo
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Info
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Dbfile
        {
            public string type { get; set; }
            public string fileName { get; set; }
            public string layout { get; set; }
        }

        public class Rule
        {
            public string logger { get; set; }
            public string minLevel { get; set; }
            public string writeTo { get; set; }
            public bool final { get; set; }
            public string level { get; set; }
        }

        public class Connectionstrings
        {
            public string DB { get; set; }
        }

        public class Kafkaconfig
        {

            public string bootstrapServers { get; set; }
            public string topic { get; set; }
            public string groupId { get; set; }

            public string username { get; set; }
            public string password { get; set; }
        }

    }
}
