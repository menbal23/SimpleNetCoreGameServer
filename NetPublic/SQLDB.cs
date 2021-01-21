using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace NetPublic
{
	public class SQLDB
	{
		static public DateTime DB_ZERO_TIME = new DateTime(1900, 1, 1);
		// 연결 문자열을 저장한다.
		public string m_Connection { get; private set; } = "";
		public SqlCommand m_Command { get; private set; } = new SqlCommand();
		private DataSet m_DataSet = new DataSet();
		private int m_CurrentTable = -1;
		public DataTable m_Table { get; private set; } = new DataTable();

		public SQLDB(string connectionString)
		{
			m_Connection = connectionString;
		}

		//DB에 접속한다.
		public bool Check()
		{
			SqlConnection conn = null;

			try
			{
				conn = new SqlConnection(m_Connection);
				conn.Open();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
			finally
			{
				if (conn != null)
				{
					conn.Close();
				}
			}

			return true;
		}

		public void SetQuery(string text)
		{
			m_Command.CommandType = CommandType.Text;
			m_Command.CommandText = text;
		}

		public void SetProcedure(string text)
		{
			m_Command.CommandType = CommandType.StoredProcedure;
			m_Command.CommandText = text;
		}

		public void Add(string name, SqlDbType type, object value)
		{
			m_Command.Parameters.Add(new SqlParameter(name, type)).Value = value;
		}

		//프로시저를 사용하여 DB에 접속한다.
		public int ExecuteQuery()
		{
			int result = 0;

			SqlConnection conn = new SqlConnection(m_Connection);
			try
			{
				conn.Open();

				m_Command.Connection = conn;

				SqlParameter ret = new SqlParameter();
				ret.Direction = ParameterDirection.ReturnValue;
				m_Command.Parameters.Add(ret);

				SqlDataAdapter adapter = new SqlDataAdapter(m_Command);
				adapter.Fill(m_DataSet);
				m_CurrentTable = -1;
				MoreResult();

				int.TryParse(ret.Value.ToString(), out result);
			}
			catch (Exception ex)
			{
				string strParameter = "";
				foreach (SqlParameter parameter in m_Command.Parameters)
				{
					if (string.IsNullOrEmpty(strParameter) == false)
						strParameter += ", ";
					strParameter += parameter.ParameterName.ToString() + "=";
					strParameter += parameter.Value != null ? parameter.Value.ToString() : "null";
				}
				Console.WriteLine(ex.Message + " : " + m_Command.CommandText + " " + strParameter);
				result = -1;
			}
			finally
			{
				if (conn != null)
					conn.Close();
			}

			return result;
		}

		public Task<int> Execute()
		{
			return Task.Run(() => { return ExecuteQuery(); });
		}

		public bool MoreResult()
		{
			if (++m_CurrentTable >= m_DataSet.Tables.Count)
				return false;

			m_Table = m_DataSet.Tables[m_CurrentTable];
			return true;
		}
	}
}
