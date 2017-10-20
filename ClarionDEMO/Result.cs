using System;

namespace ClarionDEMO
{
	public class Result
	{
		public String variableName;
		public Object x;
		public Object y;

		public Result(String variableName, Object x, Object y) {
			this.variableName = variableName;
			this.x = x;
			this.y = y;
		}

	}
}

