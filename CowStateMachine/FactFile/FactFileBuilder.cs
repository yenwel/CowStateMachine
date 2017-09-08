namespace CowStateMachine.FactFile
{
    public class FactFileBuilder
    {
        /// Method reads the filename as a CSV file, and creates the List of ExpandObjects
        /// </summary>
        /// <param name="filePath">File of CSV to be read</param>
        /// <returns>List of ExpandoObject, one object for each line in the CSV file</returns>
        public static AbstractFactFile LoadCSV(string filePath)
        {
            var header = AbstractFactFile.ReadHeader(filePath);
            AbstractFactFile @return = null;

            if (header.Contains("InterbullNumber"))
            {
                if (header.Contains("DIM"))
                {
                    @return = new MutableFactFile(filePath, header);

                }
                else
                {
                    @return = new ImmutableFactFile(filePath, header);
                }
            }
            return @return;
        }

    }
}
