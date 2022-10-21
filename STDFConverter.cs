using LinqToStdf;
using LinqToStdf.Records.V4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Virinco.WATS.Interface;

namespace STDFConverter
{
    public class STDFConverter : IReportConverter_v2
    {

        Dictionary<string, string> parameters = new Dictionary<string, string>();
        public Dictionary<string, string> ConverterParameters => parameters;

        public STDFConverter()
        {
            parameters = new Dictionary<string, string>()
            {
                {"operationTypeCode","10" }
            };
        }

        public STDFConverter(Dictionary<string, string> args)
        {
            parameters = args;
        }

        public void CleanUp()
        {
        }

        LinqToStdf.Records.V4.Far farInfo; //File Attributes
        LinqToStdf.Records.V4.Mir mirInfo; //Master Information
        LinqToStdf.Records.V4.Sdr sdrInfo; //Site Description 
        LinqToStdf.Records.V4.Pmr pmrInfo; //Pin Map
        LinqToStdf.Records.V4.Plr plrInfo; //Pin List

        public Report ImportReport(TDM api, Stream file)
        {
            var stdf = new StdfFile(api.ConversionSource.SourceFile.FullName);
            using (StreamWriter streamWriter0 = new StreamWriter(@"c:\tmp\stdfCount.txt"))
            {
                using (StreamWriter streamWriter = new StreamWriter(@"c:\tmp\stdfTypes.txt"))
                {
                    string prevType = "";
                    int typeCount = 0;

                    UUTReport uutReport = null;
                    SequenceCall currentSequence=null;
                    List<byte> waferId = new List<byte>();
                    List<byte> waferXPos = new List<byte>();
                    List<byte> waferYPos = new List<byte>();
                    int uutCount = 0;
                    foreach (var r in stdf.GetRecords())
                    {
                        Type type = r.GetType();
                        typeCount++;
                        if (prevType != type.Name)
                        {
                            streamWriter0.WriteLine($"Type={prevType} Cnt={typeCount}");
                            typeCount = 0;
                        }
                        prevType = type.Name;
                        switch (type.Name)
                        {
                            case "StartOfStreamRecord":
                                break;
                            case "EndOfStreamRecord":
                                break;
                            case "Far": //File Attributes
                                LinqToStdf.Records.V4.Far far = (LinqToStdf.Records.V4.Far)r;
                                farInfo = far;
                                DumpRecord(streamWriter, far, "Far");
                                break;
                            case "Mir": //Master Information
                                LinqToStdf.Records.V4.Mir mir = (LinqToStdf.Records.V4.Mir)r;
                                mirInfo = mir;
                                DumpRecord(streamWriter, mir, "Mir");
                                break;
                            case "Sdr": //Site Description
                                LinqToStdf.Records.V4.Sdr sdr = (LinqToStdf.Records.V4.Sdr)r;
                                sdrInfo = sdr;
                                DumpRecord(streamWriter, sdr, "Sdr");
                                break;
                            case "Pmr": //Pin Map
                                LinqToStdf.Records.V4.Pmr pmr = (LinqToStdf.Records.V4.Pmr)r;
                                pmrInfo = pmr;
                                DumpRecord(streamWriter, pmr, "Pmr");
                                break;
                            case "Plr": //Pin List
                                LinqToStdf.Records.V4.Plr Plr = (LinqToStdf.Records.V4.Plr)r;
                                plrInfo = Plr;
                                DumpRecord(streamWriter, Plr, "Plr");
                                break;
                            case "Dtr": //Datalog Text
                                LinqToStdf.Records.V4.Dtr dtr = (LinqToStdf.Records.V4.Dtr)r;
                                if (uutReport != null)
                                    AddDtr(uutReport, currentSequence, dtr);
                                DumpRecord(streamWriter, dtr, "Dtr");
                                break;
                            case "Pir": //Part Information
                                LinqToStdf.Records.V4.Pir Pir = (LinqToStdf.Records.V4.Pir)r;
                                uutCount++;
                                uutReport = CreateUUTReport(api, Pir, uutCount);
                                currentSequence=uutReport.GetRootSequenceCall();
                                waferId = new List<byte>();
                                waferXPos = new List<byte>();
                                waferYPos = new List<byte>();
                                DumpRecord(streamWriter, Pir, "Pir");
                                break;
                            case "Ptr": //Parametric Test
                                LinqToStdf.Records.V4.Ptr Ptr = (LinqToStdf.Records.V4.Ptr)r;
                                AddParametricTest(uutReport, ref currentSequence, Ptr, waferId,waferXPos,waferYPos);
                                DumpRecord(streamWriter, Ptr, "Ptr");
                                break;
                            case "Ftr": //Functional Test
                                LinqToStdf.Records.V4.Ftr Ftr = (LinqToStdf.Records.V4.Ftr)r;
                                AddFunctionalTest(uutReport, ref currentSequence, Ftr);
                                DumpRecord(streamWriter, Ftr, "Ftr");
                                break;
                            case "Prr": //Part Results
                                LinqToStdf.Records.V4.Prr Prr = (LinqToStdf.Records.V4.Prr)r;
                                //Uncomment the following line if you wish to use waferid and position as serialnumber. Remember to comment away line 259
                                //uutReport.SerialNumber = $"{BitConverter.ToInt64(waferId.ToArray(), 0):X}_{BitConverter.ToInt16(waferXPos.ToArray(),0):X}_{BitConverter.ToInt16(waferYPos.ToArray(),0):X}";
                                SubmitUUT(api, uutReport, Prr);
                                DumpRecord(streamWriter, Prr, "Prr");
                                break;
                            case "Tsr": //Test Synopsis
                                LinqToStdf.Records.V4.Tsr Tsr = (LinqToStdf.Records.V4.Tsr)r;
                                DumpRecord(streamWriter, Tsr, "Tsr");
                                break;
                            case "Hbr": //Hardware Bin
                                LinqToStdf.Records.V4.Hbr Hbr = (LinqToStdf.Records.V4.Hbr)r;
                                DumpRecord(streamWriter, Hbr, "Hbr");
                                break;
                            case "Sbr": //Software Bin
                                LinqToStdf.Records.V4.Sbr Sbr = (LinqToStdf.Records.V4.Sbr)r;
                                DumpRecord(streamWriter, Sbr, "Sbr");
                                break;
                            case "Pcr": //Part Count
                                LinqToStdf.Records.V4.Pcr Pcr = (LinqToStdf.Records.V4.Pcr)r;
                                DumpRecord(streamWriter, Pcr, "Pcr");
                                break;
                            case "Mrr": //Master Results
                                LinqToStdf.Records.V4.Mrr Mrr = (LinqToStdf.Records.V4.Mrr)r;
                                DumpRecord(streamWriter, Mrr, "Mrr");
                                break;
                            default:
                                throw new ApplicationException($"Record type {type.Name} not handled");
                        }
                    }
                }
            }
            return null;
        }

        private void AddFunctionalTest(UUTReport uutReport, ref SequenceCall sequenceCall, Ftr ftr)
        {
            Regex rex = new Regex(@"(?<SeqName>.*):\s*(?<TestName>.*)");
            Match match = rex.Match(ftr.TestText);
            if (match.Success)
            {
                if (sequenceCall.Name != match.Groups["SeqName"].Value)
                    sequenceCall = uutReport.GetRootSequenceCall().AddSequenceCall(match.Groups["SeqName"].Value);
            }
            else
                sequenceCall = uutReport.GetRootSequenceCall();
            string stepName = match.Success ? match.Groups["TestName"].Value : ftr.TestText;
            PassFailStep passFailStep = sequenceCall.AddPassFailStep($"{ftr.TestNumber} - {stepName}");
            passFailStep.AddTest(true);

            if (IsBitSet(ftr.TestFlags, 0)) passFailStep.Status = StepStatusType.Error;
            else if (IsBitSet(ftr.TestFlags, 4)) passFailStep.Status = StepStatusType.Skipped;
            else if (IsBitSet(ftr.TestFlags, 5)) passFailStep.Status = StepStatusType.Terminated;
            else if (IsBitSet(ftr.TestFlags, 6)) passFailStep.Status = StepStatusType.Done;
            else if (IsBitSet(ftr.TestFlags, 7)) passFailStep.Status = StepStatusType.Failed;
        }

        private void AddDtr(UUTReport uutReport, SequenceCall sequenceCall, Dtr dtr)
        {
            GenericStep step=sequenceCall.AddGenericStep(GenericStepTypes.Action, "Datalog");
            step.ReportText = dtr.Text;
        }

        private void AddParametricTest(UUTReport uutReport,ref SequenceCall sequenceCall,Ptr ptr, List<byte> waferId, List<byte> waferXPos, List<byte> waferYPos)
        {
            Regex rex = new Regex(@"(?<SeqName>.*):\s*(?<TestName>.*)");
            Match match=rex.Match(ptr.TestText);
            if (match.Success)
            {
                if (sequenceCall.Name != match.Groups["SeqName"].Value)
                    sequenceCall = uutReport.GetRootSequenceCall().AddSequenceCall(match.Groups["SeqName"].Value);
            }
            else
                sequenceCall=uutReport.GetRootSequenceCall();
            string stepName = match.Success ? match.Groups["TestName"].Value : ptr.TestText;
            NumericLimitStep numericLimitStep=sequenceCall.AddNumericLimitStep($"{ptr.TestNumber} - {stepName}");
            double result = (double)ptr.Result;
            if (stepName.StartsWith("Wafer ID Offset") && waferId.Count<8)
                waferId.Add((byte)result);
            if (stepName.StartsWith("Wafer X Offset") && waferXPos.Count < 2)
                waferXPos.Add((byte)result);
            if (stepName.StartsWith("Wafer Y Offset") && waferYPos.Count < 2)
                waferYPos.Add((byte)result);
            if (!IsBitSet(ptr.OptionalFlags, 0) && ptr.ResultScalingExponent > 0)
                result = result * Math.Pow(10, (double)ptr.ResultScalingExponent);

            double lowLimit = IsBitSet(ptr.OptionalFlags,6) ? double.NaN : (double)ptr.LowLimit;
            if (!double.IsNaN(lowLimit) && !IsBitSet(ptr.OptionalFlags, 4) && ptr.LowLimitScalingExponent>0)
                lowLimit = lowLimit * Math.Pow(10, (double)ptr.LowLimitScalingExponent);

            double highLimit = IsBitSet(ptr.OptionalFlags,7)? double.NaN : (double)ptr.HighLimit;
            if (!double.IsNaN(highLimit) && !IsBitSet(ptr.OptionalFlags, 5) && ptr.HighLimitScalingExponent > 0)
                highLimit = highLimit * Math.Pow(10, (double)ptr.HighLimitScalingExponent);

            if (!double.IsNaN(lowLimit) && !double.IsNaN(highLimit))
                numericLimitStep.AddTest(result, CompOperatorType.GELE, lowLimit, highLimit, ptr.Units);
            else if (!double.IsNaN(lowLimit) && double.IsNaN(highLimit))
                numericLimitStep.AddTest(result, CompOperatorType.GE, lowLimit, ptr.Units);
            else if (!double.IsNaN(highLimit) && double.IsNaN(lowLimit))
                numericLimitStep.AddTest(result, CompOperatorType.LE, highLimit, ptr.Units);
            else numericLimitStep.AddTest(result, ptr.Units);

            if (IsBitSet(ptr.TestFlags, 0)) numericLimitStep.Status = StepStatusType.Error;
            else if (IsBitSet(ptr.TestFlags, 4)) numericLimitStep.Status = StepStatusType.Skipped;
            else if (IsBitSet(ptr.TestFlags, 5)) numericLimitStep.Status = StepStatusType.Terminated;
            else if (IsBitSet(ptr.TestFlags, 6)) numericLimitStep.Status = StepStatusType.Done;
            else if (IsBitSet(ptr.TestFlags, 7)) numericLimitStep.Status = StepStatusType.Failed;          
          
        }

        private void SubmitUUT(TDM api, UUTReport uutReport, Prr prr)
        {
            if (IsBitSet(prr.PartFlag, 2))
                uutReport.Status = UUTStatusType.Terminated; 
            else if (!IsBitSet(prr.PartFlag, 4) && !IsBitSet(prr.PartFlag, 3))
                uutReport.Status = UUTStatusType.Passed;
            else if (!IsBitSet(prr.PartFlag, 4) && IsBitSet(prr.PartFlag, 3))
                uutReport.Status = UUTStatusType.Failed;
            else if (IsBitSet(prr.PartFlag, 4))
                uutReport.Status = UUTStatusType.Error; 
            uutReport.ExecutionTime = (uint)prr.TestTime / 1000;
            api.Submit(uutReport);
        }

        private UUTReport CreateUUTReport(TDM api, Pir pir, int uutCount)
        {
            UUTReport uut;
            uut = api.CreateUUTReport(mirInfo.OperatorName, mirInfo.PartType, mirInfo.DesignRevision, "WF" + uutCount.ToString(), parameters["operationTypeCode"], mirInfo.JobName, mirInfo.JobRevision);
            uut.StartDateTime = (DateTime)mirInfo.StartTime;  
            uut.StationName = mirInfo.NodeName;
            uut.BatchSerialNumber = mirInfo.LotId;
            //SerialNumber will default to "LotId + PartType + # of reports" if MIR does not contain a field with a serialnumber. Change this to what you wish to use. 
            uut.SerialNumber = string.IsNullOrEmpty(mirInfo.SerialNumber) ? mirInfo.LotId + "." + mirInfo.PartType + "#WF" + uutCount.ToString() : mirInfo.SerialNumber;
            uut.AddMiscUUTInfo("CpuType", farInfo.CpuType);
            uut.AddMiscUUTInfo("StdfVersion", farInfo.StdfVersion);
            uut.AddMiscUUTInfo("SetupTime", mirInfo.SetupTime.ToString());
            uut.AddMiscUUTInfo("ModeCode", mirInfo.ModeCode);
            uut.AddMiscUUTInfo("LotId", mirInfo.LotId);
            uut.AddMiscUUTInfo("NodeName", mirInfo.NodeName);
            uut.AddMiscUUTInfo("TesterType", mirInfo.TesterType);
            uut.AddMiscUUTInfo("ExecType", mirInfo.ExecType);
            uut.AddMiscUUTInfo("ExecVersion", mirInfo.ExecVersion);
            uut.AddMiscUUTInfo("TestTemperature", mirInfo.TestTemperature);
            uut.AddMiscUUTInfo("FamilyId", mirInfo.FamilyId);
            uut.AddMiscUUTInfo("FlowId", mirInfo.FlowId);
            uut.AddMiscUUTInfo("DesignRevision", mirInfo.DesignRevision);
            uut.AddMiscUUTInfo("HandlerType", sdrInfo.HandlerType);
            if (sdrInfo.LoadboardType!=null) uut.AddMiscUUTInfo("LoadboardType", sdrInfo.LoadboardType);
            if (sdrInfo.LoadboardId!=null) uut.AddMiscUUTInfo("LoadboardId", sdrInfo.LoadboardId);

            return uut;
        }

        string excludeProps = "RecordType,StdfFile,IsWritable,Offset,Synthesized";
        void DumpRecord(StreamWriter stream, object obj, string typeName)
        {
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.GetValue(obj, null) != null &&
                    !excludeProps.Contains(prop.Name))
                    stream.WriteLine($"{typeName} prop:  {prop.Name}, {prop.GetValue(obj, null)}");
            }
        }

        bool IsBitSet(byte? b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
    }
}
