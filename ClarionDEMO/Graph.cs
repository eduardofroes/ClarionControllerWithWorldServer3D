using System;
using System.Collections.Generic;

namespace ClarionDEMO
{
	public class Graph {
		public Graph(String title, String xTitle, String yTitle, List<Result> results){
			this.title = title;
			this.results = results;
			this.xTitle = xTitle;
			this.yTitle = yTitle;
		}

		public String title;
		public String xTitle;
		public String yTitle;
		public List<Result> results;
	}
}

