

using System;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace MTConnectAgentCore
{
	[XmlRoot("Configuration")]
	public class Configuration
	{
		
       
		private const string DEFAULT_FILE_NAME="mtcagent.xml";		
        private bool useLogFile = true;        
        private string currentXSLTHREF = "";
        private string probeXSLTHREF = "";
        private string errorXSLTHREF = "";
		
		public Configuration(Configuration source)
		{
            this.UseLogFile = source.UseLogFile;           
            this.ErrorXSLTHREF = source.ErrorXSLTHREF;
            this.ProbeXSLTHREF = source.ProbeXSLTHREF;
            this.CurrentXSLTHREF = source.CurrentXSLTHREF;
		}
		
		private void CloneConfiguration(Configuration source)
		{
            		
			this.UseLogFile=source.UseLogFile;            
            this.CurrentXSLTHREF = source.CurrentXSLTHREF;
            this.ErrorXSLTHREF = source.ErrorXSLTHREF;
            this.ProbeXSLTHREF = source.ProbeXSLTHREF;
            
		}

        public Configuration()
        {
        }

		public Configuration(bool LoadDefault)
		{
			if(LoadDefault)
			{
				TextReader xmlIn=null;
			
				try
				{
                    xmlIn = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + DEFAULT_FILE_NAME);
                    XmlSerializer s = new XmlSerializer(typeof(Configuration));
                    CloneConfiguration((Configuration)s.Deserialize(xmlIn));
		
				}
				catch(Exception e)
				{
                    LogToFile.Log("Agent Start has Failed.\n Problem with " + DEFAULT_FILE_NAME + ". " + e.Message);
                    throw new AgentException("Agent Start has Failed.\n Problem with " + DEFAULT_FILE_NAME + ". " + e.Message);
				}
				finally
				{
					if (xmlIn!=null)
						xmlIn.Close();
				}
			}
        }
        
		
		#region Internal Configuration
							
		[XmlElement("UseLogFile")]
		public bool UseLogFile
		{
			set{useLogFile=value;}
			get{return useLogFile;}
		}       

        [XmlElement("CurrentXSLTHREF")]
        public String CurrentXSLTHREF
        {
            set
            {
                currentXSLTHREF = value;
            }
            get
            {
                return currentXSLTHREF;
            }
        }

        [XmlElement("ProbeXSLTHREF")]
        public String ProbeXSLTHREF
        {
            set
            {
                probeXSLTHREF = value;
            }
            get
            {
                return probeXSLTHREF;
            }
        }

        [XmlElement("ErrorXSLTHREF")]
        public String ErrorXSLTHREF
        {
            set
            {
                errorXSLTHREF = value;
            }
            get
            {
                return errorXSLTHREF;
            }
        }	
        #endregion

    }
}