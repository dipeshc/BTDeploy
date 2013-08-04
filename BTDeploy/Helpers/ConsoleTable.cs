using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTDeploy.Helpers
{
	public class ConsoleTable
	{
		protected readonly int NumberOfColumns;
		protected readonly List<List<string>> Rows;

		public ConsoleTable(params string[] columnHeadings)
		{
			NumberOfColumns = columnHeadings.Count ();
			Rows = new List<List<string>> ();
			Rows.Add (columnHeadings.ToList ());
		}

		public void AddRow(params string[] rowFields)
		{
			if(rowFields.Count() != NumberOfColumns)
				throw new ArgumentOutOfRangeException(string.Format("Expected {0} fields, got {1} arguments.", NumberOfColumns, rowFields.Count()));

			Rows.Add (rowFields.ToList ());
		}

		public override string ToString ()
		{
			var output = new StringBuilder ();

			var columnSizes = new List<int> (NumberOfColumns);
			for (var column = 0; column!=NumberOfColumns; ++column)
			{
				var size = Rows.Select (row => row.ElementAt (column)).Max (field => field.Length);
				columnSizes.Insert (column, size);
			}

			var formatString = "|";
			for(var i = 0; i!=columnSizes.Count(); ++i)
			{
				formatString += " {"+i+"} |";
			}

			for(var rowNumber = 0; rowNumber!=Rows.Count(); ++rowNumber)
			{
				var row = Rows [rowNumber];
				var paddedRowFields = row.Select ((field, i) => row [i].PadRight (columnSizes [i]));
				var rowOutput = string.Format(formatString, paddedRowFields.ToArray ());

				if(rowNumber == 0)
					output.AppendLine(new string('-', rowOutput.Length));

				output.AppendLine (rowOutput);

				if(rowNumber == 0 || rowNumber == Rows.Count() - 1)
					output.AppendLine(new string('-', rowOutput.Length));
			};

			return output.ToString();
		}
	}
}