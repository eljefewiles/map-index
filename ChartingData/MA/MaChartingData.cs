using MapIndex.Models.MA;
using MapIndex.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Dynamic;

namespace MapIndex.ChartingData.MA
{
    public class MaChartingData : BaseChartingData<MaData>, IChartingData<MaData>
    {

       
       
        IChartOutput IChartingData<MaData>.GetChartData(List<MaData> data, MapSearch map)
        {
            Models.Charting.ChartOptions chart = new Models.Charting.ChartOptions();

            try
            {
                chart.ChartXCategories = new List<Models.Charting.ChartXAxis>();

                //Dictionary<string, string> q = new Dictionary<string, string>();

                ////var groups = data.OrderByDescending(s => s.Date_Announced.Value).GroupBy(item => ((item.Date_Announced.Value.Month - 1) / 3));

                //var groupedByQuarter = from date in data
                //                       group date by (date.Date_Announced.Value.Month - 1) / 3
                //                        into groupedDates
                //                       orderby groupedDates.Key
                //                       select groupedDates;

                //foreach (var quarter in groupedByQuarter)
                //{
                //    q.Add("Q" + quarter.Key, string.Join(",", quarter.Select(s => s.Date_Announced.Value.Year).Distinct()));
                //    // {0} --> Quarter
                //    // quarter --> Date
                //    //Console.WriteLine("Q: {0}, Dates: {1}", quarter.Key + 1, string.Join(", ", quarter));
                //}
                data = data.Where(s => s.Date_Announced >= Convert.ToDateTime("01/01/2016")).ToList();
                // base list of years and quarters
                var query = (from t in data
                             group t by new { t.Date_Announced.Value.Year, Quarter = ((t.Date_Announced.Value.Month - 1) / 3) + 1 }
                 into grp
                             select new
                             {
                                 grp.Key.Year,
                                 grp.Key.Quarter
                                 ///Quantity = grp.Sum(t => Convert.ToDouble(t.Value___MM_))
                             }).ToList();

                // if data is found in the quarter/year combo

                foreach (var item in query)
                {
                    chart.ChartXCategories.Add(new Models.Charting.ChartXAxis()
                    {
                        Category = item.Quarter.ToString() + item.Year.ToString(),
                        Year = item.Year.ToString(),
                        Quarter = item.Quarter.ToString()
                    });
                }


                // MUST HAVE EQUIVALENT CHART SERIES VALUES FOR EACH NAME FOR THE CHART TO WORK
                int countOfXCategories = 0;
                chart.ChartSeries = new List<Models.Charting.Chart>();
                var distinctPlays = data.Select(s => s.US_Play).OrderBy(s => s).Distinct();

                int i = 1;
                foreach (var item in distinctPlays)
                {
                    int id = i;
                    string play = item;
                    List<double> values = new List<double>();
                    foreach (var a in chart.ChartXCategories)
                    {

                        double number = data.Where(t => ((t.Date_Announced.Value.Month - 1) / 3 + 1) == Convert.ToInt32(a.Quarter)
                        && t.Date_Announced.Value.Year == Convert.ToInt32(a.Year) && t.US_Play == item && t.Value___MM_ != null)
                        .Sum(s => Convert.ToDouble(s.Value___MM_));
                        values.Add(number);


                    }
                    chart.ChartSeries.Add(new Models.Charting.Chart()
                    { Id = i, Name = item, Values = values }
                        );
                    i++;
                }

            }
            catch (Exception ex)
            {

            }
                       

            return chart as IChartOutput;
        }

        internal virtual List<Models.Charting.Chart> ChartData()
        {
            List<Models.Charting.Chart> c = new List<Models.Charting.Chart>();



            return c;
        }
        
    }
}