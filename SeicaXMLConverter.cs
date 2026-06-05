using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.Seica
{
    public class SeicaXMLConverter : IReportConverter_v2
    {
        Dictionary<string, string> parameters;
        public SeicaXMLConverter() : base()
        {
            parameters = new Dictionary<string, string>()
            {
                {"operationTypeCode","10" },
                {"sequenceName","SoftwareName" },
                {"sequenceVersion","1.0.0" }
            };
        }

        public Dictionary<string, string> ConverterParameters => parameters;

        public SeicaXMLConverter(Dictionary<string, string> args)
        {
            parameters = args;
        }

        public void CleanUp()
        {
        }


        private StepStatusType GetStepStatusType(string status)
        {
            if (status == "0")
            {
                return StepStatusType.Passed;
            }
            else
            {
                return StepStatusType.Failed;
            }
        }

        private UUTStatusType GetUUTStatusType(string status)
        {
            if (status == "0")
            {
                return UUTStatusType.Passed;
            }
            else
            {
                return UUTStatusType.Failed;
            }
        }

        public Report ImportReport(TDM api, Stream file)
        {

            using (StreamReader reader = new StreamReader(file))
            {
                XDocument xmlReport = XDocument.Load(reader);
                Report WATSReport = ReadReport(xmlReport, api);
                return WATSReport;
            }
        }


        private Report ReadReport(XDocument xmlReportDoc, TDM api)
        {

            api.TestMode = TestModeType.Active;

            // Sections
            XElement xmlR = xmlReportDoc.Element("R");
            XElement xmlPrgC = xmlR.Element("PrgC");
            XElement xmlST = xmlR.Element("ST");
            XElement xmlBI = xmlR.Element("BI");
            XElement xmlET = xmlR.Element("ET");

            // Create UUT
            string nm = xmlST.Attribute("NM").Value;
            string[] splitted_nm = Regex.Split(nm, @"[_\s]");


            UUTReport uut = api.CreateUUTReport(
                            xmlST.Attribute("OP").Value,
                            splitted_nm[0],
                            splitted_nm[1],
                            xmlBI.Attribute("BCP").Value,
                            ConverterParameters["operationTypeCode"],
                            ConverterParameters["sequenceName"],
                            ConverterParameters["sequenceVersion"]);


            string startDateString = xmlBI.Attribute("SD").Value;
            string endDateString = xmlET.Attribute("ED").Value;
            string format = "dd-MM-yyyy HH:mm:ss";
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime parsedStartDate = DateTime.ParseExact(startDateString, format, provider);
            DateTime parsedEndDate = DateTime.ParseExact(endDateString, format, provider);

            TimeSpan difference = parsedEndDate - parsedStartDate;

            var totalSeconds = (double)difference.TotalSeconds;

            uut.StartDateTime = parsedStartDate;
            uut.ExecutionTime = totalSeconds;

            string boardName = xmlST.Attribute("NMP").Value;
            uut.AddMiscUUTInfo("Board Name", boardName);

            var tests = xmlBI.Elements("TEST");


            string currentGroup = "";
            SequenceCall currentSequence = null;

            foreach (var test in tests)
            {

                var testGroup = test.Attribute("F").Value;
                var stepName = test.Attribute("NM").Value;
                var measurement = double.Parse(test.Attribute("MR").Value, CultureInfo.InvariantCulture);
                var lowerLimit = double.Parse(test.Attribute("ML").Value, CultureInfo.InvariantCulture);
                var upperLimit = double.Parse(test.Attribute("MH").Value, CultureInfo.InvariantCulture);
                var unit = test.Attribute("MU").Value;
                var status = test.Attribute("TR").Value;
                var testTime = double.Parse(test.Attribute("TT").Value, CultureInfo.InvariantCulture);

                if (currentGroup == "")
                {
                    currentGroup = testGroup;
                    currentSequence = uut.GetRootSequenceCall().AddSequenceCall(testGroup);

                }
                else if (testGroup != currentGroup)
                {
                    currentGroup = testGroup;
                    currentSequence = uut.GetRootSequenceCall().AddSequenceCall(testGroup);

                }

                NumericLimitStep currentStep = currentSequence.AddNumericLimitStep(stepName);

                currentStep.StepTime = testTime / 1000;

                NumericLimitTest currentTest = currentStep.AddTest(measurement, CompOperatorType.GELE, lowerLimit, upperLimit, unit, GetStepStatusType(status));

            }

            var uutStatusString = xmlET.Attribute("NF").Value;

            uut.Status = GetUUTStatusType(uutStatusString);

            return uut;
        }
    }
}
