using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppCheckLifeTimeFiles.Models.Configuration
{
    public class FolderElement : ConfigurationElement
    {        
        [ConfigurationProperty("folderKey", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string FolderKey
        {
            get { return ((string)(base["folderKey"])); }
            set { base["folderKey"] = value; }
        }

        [ConfigurationProperty("path", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string Path
        {
            get { return ((string)(base["path"])); }
            set { base["path"] = value; }
        }
    }
}
