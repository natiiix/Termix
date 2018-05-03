using System.Collections.Generic;
using System.IO;

namespace Termix
{
    public class AssistantData
    {
        private readonly string dataDirPath;

        private string AliasFilePath { get => dataDirPath + "alias.txt"; }
        private string FacebookFilePath { get => dataDirPath + "facebook.txt"; }
        private string CommandFilePath { get => dataDirPath + "command.txt"; }

        public DataAlias[] Aliases { get; private set; }
        public DataAlias[] FacebookContacts { get; private set; }
        public DataAlias[] UserCommands { get; private set; }

        public AssistantData(string dataDirPath)
        {
            this.dataDirPath = dataDirPath;

            Directory.CreateDirectory(dataDirPath);

            Aliases = GetAliasesFromFile(AliasFilePath);
            FacebookContacts = GetAliasesFromFile(FacebookFilePath);
            UserCommands = GetAliasesFromFile(CommandFilePath);
        }

        private static DataAlias[] GetAliasesFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                List<DataAlias> aliases = new List<DataAlias>();

                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    try
                    {
                        DataAlias alias = new DataAlias(line);
                        aliases.Add(alias);
                    }
                    catch { }
                }

                return aliases.ToArray();
            }
            else
            {
                File.Create(filePath);
                return new DataAlias[0];
            }
        }
    }
}
