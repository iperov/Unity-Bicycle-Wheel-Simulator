namespace sys.debug
{
    public enum ELogType
    {
        none,
        info,
        warning,
        error
    };

    public class Log
    {
        static public Log g;


        const int m_max_lines = 80;
        int m_line_counter;
        string[] m_lines = new string[m_max_lines];

        public Log()
        {
            g = this;
        }

        public void add (string text, ELogType lt = ELogType.info )
        {
            m_lines[m_line_counter++ % (uint) m_max_lines] = text;
        }

        public int getLineCount() { return m_max_lines; }

        public string getLine(int line)
        {
            return m_lines[ (m_line_counter +line) % (uint) m_max_lines ];
        }
    }
}
