using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.IO;

namespace PgSqlClient
{
    public class SqlCommandParser
    {
        private readonly List<String> _commandList = new List<string>();

        public SqlCommandParser(String filename)
        {
	        using (var sr = File.OpenText(filename))
            {
                var sql = sr.ReadToEnd();
                var r = new Regex("[^;]*;");
                var mc = r.Matches(sql);

                for (var i = 0; i < mc.Count; i++)
                {
                    _commandList.Add(mc[i].Value);
                }
            }
        }

        public ReadOnlyCollection<String> Commands
        {
            get { return _commandList.AsReadOnly(); }
        }
    }
}
