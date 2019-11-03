namespace RoslynCodeGeneratorTest.TypeMapping
{
    internal struct DataTypeMap
    {
        public string DataClass { get; set; }
        public int Id { get; set; }
        public bool Analogue { get; set; }

        public DataTypeMap(string dataClass, int id, bool analogue)
        {
            DataClass = dataClass;
            Id = id;
            Analogue = analogue;
        }
    }
}
