﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DanpheEMR.Core.Configuration;
using DanpheEMR.ServerModel;
using DanpheEMR.DalLayer;
using System.Data.Entity;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using DanpheEMR.Utilities;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http.Features;
using DanpheEMR.CommonTypes;
using DanpheEMR.Core.Caching;
using System.Xml;
using DanpheEMR.Security;
using DanpheEMR.ServerModel.LabModels;

namespace DanpheEMR.Controllers
{

    public class LabController : CommonController
    {
        //private bool docPatPortalSync = false;
        public LabController(IOptions<MyConfiguration> _config) : base(_config)
        {
            //docPatPortalSync = _config.Value.DanphePatientPortalSync;
        }

        // GET: api/values
        [HttpGet]
        public string Get(string reqType,
            int? SampleCode,
            int requisitionId,
            int patientId,
            int labReportId,
            string inputValue,
            int templateId,
            int patientVisitId,
            int employeeId,
            string labTestSpecimen,
            DateTime SampleDate,
            string requisitionIdList,
            string runNumberType,
            int barCodeNumber,
            string wardName,
            string visitType,
            string formattedSampleCode,
            DateTime FromDate,
            DateTime ToDate,
            DateTime date
            )
        {

            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                LabDbContext labDbContext = new LabDbContext(connString);


                //it is used in collect sample page
                //sud: 15Sept'18--we're excluding IsActive = false requisitions from Lab_TestRequisitionTable
                if (reqType == "labRequisition")//!=null not needed for string.
                {
                    //Removed to show all detail regardless of BillingStatus
                    //&& (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                    var histoPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                         join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                         where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                         && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "histo")
                                         select new
                                         {
                                             RequisitionId = req.RequisitionId,
                                             PatientId = req.PatientId,
                                             PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                             PatientCode = req.Patient.PatientCode,
                                             DateOfBirth = req.Patient.DateOfBirth,
                                             Gender = req.Patient.Gender,
                                             PhoneNumber = req.Patient.PhoneNumber,
                                             LastestRequisitionDate = req.OrderDateTime,
                                             VisitType = req.VisitType,
                                             RunNumberType = req.RunNumberType,
                                             WardName = req.WardName
                                         }).OrderByDescending(a => a.LastestRequisitionDate).ToList();

                    //Removed to show all detail regardless of BillingStatus
                    //&& (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                    var cytoPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                        join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                        where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                        && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "cyto")
                                        select new
                                        {
                                            RequisitionId = req.RequisitionId,
                                            PatientId = req.PatientId,
                                            PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                            PatientCode = req.Patient.PatientCode,
                                            DateOfBirth = req.Patient.DateOfBirth,
                                            Gender = req.Patient.Gender,
                                            PhoneNumber = req.Patient.PhoneNumber,
                                            LastestRequisitionDate = req.OrderDateTime,
                                            VisitType = req.VisitType,
                                            RunNumberType = req.RunNumberType,
                                            WardName = req.WardName
                                        }).OrderByDescending(a => a.LastestRequisitionDate).ToList();

                    //.OrderByDescending(a => a.LatestRequisitionDate).ToList()

                    //Removed to show all detail regardless of BillingStatus
                    //&& (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                    var normalPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                          join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                          //show only paid and unpaid requisitions in the list.
                                          //show only IsActive=True and IsActive=NULL requests, Hide IsActive=False. -- sud: 15Sept'18
                                          //if IsActive has value then it should be true, if it's null then its true by default. 
                                          where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                          && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "normal")
                                          group req by new { req.Patient, req.VisitType, req.WardName } into p
                                          select new
                                          {
                                              RequisitionId = (long)0,
                                              PatientId = p.Key.Patient.PatientId,
                                              PatientName = p.Key.Patient.FirstName + " " + (string.IsNullOrEmpty(p.Key.Patient.MiddleName) ? "" : p.Key.Patient.MiddleName + " ") + p.Key.Patient.LastName,
                                              PatientCode = p.Key.Patient.PatientCode,
                                              DateOfBirth = p.Key.Patient.DateOfBirth,
                                              Gender = p.Key.Patient.Gender,
                                              PhoneNumber = p.Key.Patient.PhoneNumber,
                                              LastestRequisitionDate = p.Max(r => r.OrderDateTime),
                                              VisitType = p.Key.VisitType,
                                              RunNumberType = "normal",
                                              WardName = p.Key.WardName
                                              //IsAdmitted = (from adm in labDbContext.Admissions
                                              //              where adm.PatientId == p.Key.Patient.PatientId && adm.AdmissionStatus == "admitted"
                                              //              select adm.AdmissionStatus).FirstOrDefault() == null ? true : false
                                          }).OrderByDescending(b => b.LastestRequisitionDate).ToList();


                    var combined = histoPatients.Union(cytoPatients).Union(normalPatients);
                    responseData.Results = combined.OrderByDescending(c => c.LastestRequisitionDate);

                }

                //getting the test of selected patient 
                else if (reqType == "LabSamplesByPatientId")
                {

                    //include patien ---------------------------------
                    var result = (from req in labDbContext.Requisitions.Include("Patient")
                                  join labTest in labDbContext.LabTests on req.LabTestId equals labTest.LabTestId
                                  //show only IsActive=True and IsActive=NULL requests, Hide IsActive=False. -- sud: 15Sept'18
                                  //if IsActive has value then it should be true, if it's null then its true by default. 
                                  where req.PatientId == patientId && (req.IsActive.HasValue ? req.IsActive.Value == true : true) &&
                                  (req.BillingStatus == "paid" && req.OrderStatus == "active")
                                  && (req.VisitType.ToLower() == visitType.ToLower())
                                  && (req.RunNumberType.ToLower() == runNumberType.ToLower())

                                  select new PatientLabSampleVM
                                  {
                                      PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                      OrderStatus = req.OrderStatus,
                                      SpecimenList = labTest.LabTestSpecimen,
                                      RequisitionId = req.RequisitionId,
                                      TestName = req.LabTestName,
                                      SampleCode = req.SampleCode,
                                      OrderDateTime = req.OrderDateTime,
                                      SampleCreatedOn = req.SampleCreatedOn,
                                      ProviderName = req.ProviderName

                                  }).ToList();
                    if (result.Count != 0)
                    {
                        result.ForEach(res =>
                        {
                            //string specimen = res.Specimen.Split('/')[0];
                            var dateTime = DateTime.Parse(res.OrderDateTime.ToString()).AddHours(-24);

                            if (res.SampleCode == null)
                            {
                                var lastTest = (from labReq in labDbContext.Requisitions
                                                join labTest in labDbContext.LabTests on labReq.LabTestId equals labTest.LabTestId
                                                where labReq.PatientId == patientId
                                                      && res.SpecimenList.Contains(labReq.LabTestSpecimen)
                                                      && labReq.SampleCreatedOn > dateTime
                                                select new
                                                {
                                                    SampleCode = labReq.SampleCode,
                                                    SampleCreatedOn = labReq.SampleCreatedOn,
                                                    SampleCreatedBy = labReq.SampleCreatedBy,
                                                    LabTestSpecimen = labReq.LabTestSpecimen
                                                }).OrderByDescending(a => a.SampleCreatedOn).ThenByDescending(a => a.SampleCode).FirstOrDefault();
                                if (lastTest != null)
                                {
                                    res.LastSampleCode = DateTime.Parse(lastTest.SampleCreatedOn.ToString()).ToString("yyMMdd") + "-" + lastTest.SampleCode;
                                    res.SampleCreatedOn = lastTest.SampleCreatedOn;
                                    res.SampleCreatedBy = lastTest.SampleCreatedBy;
                                    res.LastSpecimenUsed = lastTest.LabTestSpecimen;
                                }
                            }

                        });
                    }

                    responseData.Results = result;
                }

                //getting the test of selected patient 
                else if (reqType == "LabSamplesWithCodeByPatientId")
                {
                    List<PatientLabSampleVM> result = new List<PatientLabSampleVM>();

                    //include patien ---------------------------------
                    if (requisitionId == 0)
                    {

                        result = (from req in labDbContext.Requisitions.Include("Patient")
                                  join labTest in labDbContext.LabTests on req.LabTestId equals labTest.LabTestId
                                  //show only IsActive=True and IsActive=NULL requests, Hide IsActive=False. -- sud: 15Sept'18
                                  //if IsActive has value then it should be true, if it's null then its true by default. 
                                  where req.PatientId == patientId && (req.IsActive.HasValue ? req.IsActive.Value == true : true) &&
                                  (wardName == "null" ? req.WardName == null : req.WardName == wardName) &&
                                  (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned")
                                  && req.OrderStatus == "active"
                                  && (req.VisitType.ToLower() == visitType.ToLower())
                                  && req.RunNumberType.ToLower() == runNumberType.ToLower()

                                  select new PatientLabSampleVM
                                  {
                                      PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                      OrderStatus = req.OrderStatus,
                                      SpecimenList = labTest.LabTestSpecimen,
                                      RequisitionId = req.RequisitionId,
                                      TestName = req.LabTestName,
                                      SampleCode = req.SampleCode,
                                      OrderDateTime = req.OrderDateTime,
                                      SampleCreatedOn = req.SampleCreatedOn,
                                      RunNumberType = req.RunNumberType,
                                      ProviderName = req.ProviderName,
                                      HasInsurance = req.HasInsurance//sud:16Jul'19--to show insurance flag in sample collection and other pages.
                                  }).ToList();
                    }
                    else
                    {
                        var reqId = (long)requisitionId;
                        result = (from req in labDbContext.Requisitions.Include("Patient")
                                  join labTest in labDbContext.LabTests on req.LabTestId equals labTest.LabTestId
                                  //show only IsActive=True and IsActive=NULL requests, Hide IsActive=False. -- sud: 15Sept'18
                                  //if IsActive has value then it should be true, if it's null then its true by default. 
                                  where req.PatientId == patientId && (req.IsActive.HasValue ? req.IsActive.Value == true : true)
                                  && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned")
                                  && req.OrderStatus == "active"
                                  && (req.VisitType.ToLower() == visitType.ToLower())
                                  && (req.RunNumberType.ToLower() == runNumberType.ToLower())
                                  && (req.RequisitionId == reqId)

                                  select new PatientLabSampleVM
                                  {
                                      PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                      OrderStatus = req.OrderStatus,
                                      SpecimenList = labTest.LabTestSpecimen,
                                      RequisitionId = req.RequisitionId,
                                      TestName = req.LabTestName,
                                      SampleCode = req.SampleCode,
                                      OrderDateTime = req.OrderDateTime,
                                      SampleCreatedOn = req.SampleCreatedOn,
                                      RunNumberType = req.RunNumberType,
                                      ProviderName = req.ProviderName,
                                      HasInsurance = req.HasInsurance
                                  }).ToList();
                    }


                    if (result.Count != 0)
                    {
                        result.ForEach(res =>
                        {
                            //string specimen = res.Specimen.Split('/')[0];
                            //var dateTime = DateTime.Parse(res.OrderDateTime.ToString()).AddHours(-24);

                            DateTime sampleDate = DateTime.Now;


                            //if (billingType != null && billingType.ToLower() == "outpatient")
                            //{
                            //    if (res.RunNumberType.ToLower() == "normal")
                            //    {
                            //        string sampleCodeFormatted = GetSampleCodeFormatted(GetOutpatientLatestSampleSequence(labDbContext, sampleDate), sampleDate, billingType);
                            //        if (sampleCodeFormatted != null)
                            //        {
                            //            string[] sampleCodeBreakup = sampleCodeFormatted.Split('/');
                            //            res.SmCode = sampleCodeBreakup[1];
                            //            res.SmNumber = Convert.ToInt32(sampleCodeBreakup[0]);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        res.SmCode = DanpheDateConvertor.ConvertEngToNepDate(System.DateTime.Now).Year.ToString();
                            //        string last3Dig = res.SmCode.Substring(1, 3);
                            //        res.SmCode = last3Dig;
                            //        var samplNum = GetYearlyTypeLatestSampleSequence(labDbContext, res.RunNumberType.ToLower());
                            //        res.SmNumber = samplNum.HasValue ? (int)samplNum : 0; 
                            //    }

                            //}
                            //else if (billingType != null && billingType.ToLower() == "inpatient")
                            //{
                            //    if (res.RunNumberType.ToLower() == "normal")
                            //    {
                            //        res.SmCode = DanpheDateConvertor.ConvertEngToNepDate(System.DateTime.Now).Year.ToString();
                            //        string last3Dig = res.SmCode.Substring(1, 3);
                            //        res.SmCode = last3Dig;
                            //        res.SmNumber = (int)GetInpatientLatestSampleSequence(labDbContext);
                            //    }
                            //    else
                            //    {
                            //        res.SmCode = DanpheDateConvertor.ConvertEngToNepDate(System.DateTime.Now).Year.ToString();
                            //        string last3Dig = res.SmCode.Substring(1, 3);
                            //        res.SmCode = last3Dig;
                            //        var samplNum = GetYearlyTypeLatestSampleSequence(labDbContext, res.RunNumberType.ToLower());
                            //        res.SmNumber = samplNum.HasValue ? (int)samplNum : 0;
                            //    }
                            //}


                        });
                    }

                    responseData.Results = result;
                }


                //getting latest sample code
                else if (reqType == "latest-samplecode")
                {
                    DateTime sampleDate = SampleDate != null ? SampleDate : DateTime.Now;

                    var RunType = runNumberType;
                    var barCodeList = (from v in labDbContext.LabBarCode
                                       select v).ToList();
                    var BarCodeNumber = (barCodeList.Count > 0) ? barCodeList.Max(val => val.BarCodeNumber) + 1 : 1000000;



                    if (visitType != null && visitType.ToLower() == "outpatient")
                    {
                        if (RunType.ToLower() == "normal")
                        {
                            string sampleCodeFormatted = GetSampleCodeFormatted(GetOutpatientLatestSampleSequence(labDbContext, sampleDate), sampleDate, visitType, RunType);
                            if (sampleCodeFormatted != null)
                            {
                                string[] sampleCodeBreakup = sampleCodeFormatted.Split('/');
                                responseData.Results = new { SampleCode = sampleCodeBreakup[1], SampleNumber = sampleCodeBreakup[0], BarCodeNumber = BarCodeNumber };
                            }
                        }
                        else
                        {
                            var samCollDate = DanpheDateConvertor.ConvertEngToNepDate(sampleDate).Year.ToString();
                            string last3Dig = samCollDate.Substring(1, 3);
                            var samplNum = (int)GetYearlyTypeLatestSampleSequence(labDbContext, RunType.ToLower());
                            responseData.Results = new { SampleCode = last3Dig, SampleNumber = samplNum, BarCodeNumber = BarCodeNumber };
                        }


                    }
                    else if (visitType != null && (visitType.ToLower() == "inpatient" || visitType.ToLower() == "emergency"))
                    {
                        if (RunType.ToLower() == "normal")
                        {
                            string sampleCodeFormatted = GetSampleCodeFormatted(GetInpatientLatestSampleSequence(labDbContext), sampleDate, visitType, RunType);
                            if (sampleCodeFormatted != null)
                            {
                                string[] sampleCodeBreakup = sampleCodeFormatted.Split('/');
                                responseData.Results = new { SampleCode = sampleCodeBreakup[1], SampleNumber = sampleCodeBreakup[0], BarCodeNumber = BarCodeNumber };
                            }

                        }
                        else
                        {
                            var samCollDate = DanpheDateConvertor.ConvertEngToNepDate(sampleDate).Year.ToString();
                            string last3Dig = samCollDate.Substring(1, 3);
                            var samplNum = (int)GetYearlyTypeLatestSampleSequence(labDbContext, RunType.ToLower());
                            responseData.Results = new { SampleCode = last3Dig, SampleNumber = samplNum, BarCodeNumber = BarCodeNumber };
                        }

                    }


                    responseData.Status = "OK";
                }

                else if (reqType == "check-samplecode")
                {
                    SampleDate = SampleDate != null ? SampleDate : DateTime.Now;

                    LabRequisitionModel requisition = null;
                    var RunNumberType = runNumberType;

                    if (visitType.ToLower() == "inpatient" || visitType.ToLower() == "emergency")
                    {
                        if (RunNumberType.ToLower() == "normal")
                        {
                            requisition = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue) //this already filters not null data.. 
                                           where sam.SampleCode == SampleCode && SampleDate.Date.Year == sam.SampleCreatedOn.Value.Year
                                           && (sam.VisitType.ToLower() == "inpatient" || sam.VisitType.ToLower() == "emergency") && sam.RunNumberType.ToLower() == RunNumberType
                                           select sam).FirstOrDefault();
                        }
                        else
                        {
                            requisition = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue)
                                           where sam.SampleCode == SampleCode && SampleDate.Date.Year == sam.SampleCreatedOn.Value.Year
                                           && sam.RunNumberType.ToLower() == RunNumberType
                                           select sam).FirstOrDefault();
                        }

                    }
                    else if (visitType.ToLower() == "outpatient")
                    {
                        if (RunNumberType.ToLower() == "normal")
                        {
                            requisition = (from sam in labDbContext.Requisitions
                                           where sam.SampleCode == SampleCode
                                           && DbFunctions.TruncateTime(sam.SampleCreatedOn).Value == DbFunctions.TruncateTime(SampleDate).Value
                                           && sam.VisitType.ToLower() == "outpatient" && sam.RunNumberType.ToLower() == RunNumberType
                                           select sam).FirstOrDefault();
                        }
                        else
                        {
                            requisition = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue)
                                           where sam.SampleCode == SampleCode && SampleDate.Date.Year == sam.SampleCreatedOn.Value.Year
                                           && sam.RunNumberType.ToLower() == RunNumberType
                                           select sam).FirstOrDefault();
                        }

                    }
                    if (requisition != null)
                    {
                        responseData.Results = new { Exist = true, PatientName = requisition.PatientName, PatientId = requisition.PatientId, SampleCreatedOn = requisition.SampleCreatedOn };
                    }
                    else
                    {
                        responseData.Results = new { Exist = false };
                    }

                    responseData.Status = "OK";
                }


                else if (reqType == "lastSampleCode")
                {
                    var currTest = (from labReq in labDbContext.Requisitions
                                    where labReq.RequisitionId == requisitionId
                                    select labReq).FirstOrDefault();

                    var dateTime = DateTime.Parse(currTest.OrderDateTime.ToString()).AddHours(-24);

                    var result = (from lastTest in labDbContext.Requisitions
                                  where lastTest.PatientId == currTest.PatientId
                                        && lastTest.LabTestSpecimen == labTestSpecimen
                                        && lastTest.SampleCreatedOn > dateTime
                                  select new
                                  {
                                      SampleCode = lastTest.SampleCode,
                                      SampleCreatedOn = lastTest.SampleCreatedOn,
                                      SampleCreatedBy = lastTest.SampleCreatedBy,
                                      LabTestSpecimen = lastTest.LabTestSpecimen
                                  }).OrderByDescending(a => a.SampleCreatedOn).ThenByDescending(a => a.SampleCode).FirstOrDefault();
                    responseData.Results = result;
                }
                //getting the data from requisition and component to add 
                //in the labresult service ....(used in collect-sample.component)
                else if (reqType == "pendingLabResults")
                {
                    List<LabPendingResultVM> results = new List<LabPendingResultVM>();
                    var reportWithHtmlTemplate = (from req in labDbContext.Requisitions
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus.ToLower() == "pending"
                                                  && req.SampleCode != null
                                                  && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                  && (template.TemplateType.ToLower() == "html")
                                                  //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                  group new { req, test, template } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RequisitionId,
                                                      req.RunNumberType,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      Tests = grp.Select(a =>
                                                      new LabPendingResultVM.LabTestDetail()
                                                      {
                                                          RequisitionId = a.req.RequisitionId,
                                                          TestName = a.test.LabTestName,
                                                          LabTestId = a.test.LabTestId,
                                                          ReportTemplateId = a.template.ReportTemplateID,
                                                          ReportTemplateShortName = a.template.ReportTemplateShortName,
                                                          RunNumberType = a.req.RunNumberType
                                                      }).OrderBy(req => req.RequisitionId).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();

                    var reportWithNormalEntry = (from req in labDbContext.Requisitions
                                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                 join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                 where req.OrderStatus.ToLower() == "pending"
                                                 && req.SampleCode != null
                                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                 //Removed as all can add result but cannot Print Report Until Bill is Paid (incase of OP)
                                                 //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                 && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                 //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                 group new { req, test, template } by new
                                                 {
                                                     patient,
                                                     req.SampleCode,
                                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                     req.VisitType,
                                                     req.RunNumberType,
                                                     req.BarCodeNumber,
                                                     req.WardName
                                                 } into grp
                                                 select new LabPendingResultVM
                                                 {
                                                     PatientId = grp.Key.patient.PatientId,
                                                     PatientCode = grp.Key.patient.PatientCode,
                                                     DateOfBirth = grp.Key.patient.DateOfBirth,
                                                     PhoneNumber = grp.Key.patient.PhoneNumber,
                                                     Gender = grp.Key.patient.Gender,
                                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                     SampleCode = grp.Key.SampleCode,
                                                     SampleDate = grp.Key.Value,
                                                     VisitType = grp.Key.VisitType,
                                                     RunNumType = grp.Key.RunNumberType,
                                                     BarCodeNumber = grp.Key.BarCodeNumber,
                                                     WardName = grp.Key.WardName,
                                                     Tests = grp.Select(a =>
                                                     new LabPendingResultVM.LabTestDetail()
                                                     {
                                                         RequisitionId = a.req.RequisitionId,
                                                         TestName = a.test.LabTestName,
                                                         LabTestId = a.test.LabTestId,
                                                         ReportTemplateId = a.template.ReportTemplateID,
                                                         ReportTemplateShortName = a.template.ReportTemplateShortName
                                                     }).OrderBy(req => req.RequisitionId).ToList()
                                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



                    foreach (var rep in reportWithHtmlTemplate)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        results.Add(rep);
                    }
                    foreach (var repNormal in reportWithNormalEntry)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        results.Add(repNormal);
                    }

                    responseData.Results = results.OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();
                }

                else if (reqType == "pending-reports")
                {
                    List<LabPendingResultVM> results = new List<LabPendingResultVM>();

                    var verificationParameter = (from param in labDbContext.AdminParameters
                                                 where param.ParameterGroupName.ToLower() == "lab" && param.ParameterName == "LabReportVerificationNeededB4Print"
                                                 select param.ParameterValue).FirstOrDefault();

                    bool verificationRequired = Convert.ToBoolean(verificationParameter);


                    var pendingNormalReports = (from req in labDbContext.Requisitions
                                                join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                                && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                group new { req, template, patient, test } by new
                                                {
                                                    patient,
                                                    req.SampleCode,
                                                    DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                    req.VisitType,
                                                    req.RunNumberType,
                                                    req.BarCodeNumber,
                                                    req.WardName
                                                } into grp
                                                select new LabPendingResultVM
                                                {
                                                    PatientId = grp.Key.patient.PatientId,
                                                    PatientCode = grp.Key.patient.PatientCode,
                                                    DateOfBirth = grp.Key.patient.DateOfBirth,
                                                    PhoneNumber = grp.Key.patient.PhoneNumber,
                                                    Gender = grp.Key.patient.Gender,
                                                    PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                    SampleCode = grp.Key.SampleCode,
                                                    SampleDate = grp.Key.Value,
                                                    VisitType = grp.Key.VisitType,
                                                    RunNumType = grp.Key.RunNumberType,
                                                    BarCodeNumber = grp.Key.BarCodeNumber,
                                                    WardName = grp.Key.WardName,
                                                    Tests = (from requisition in labDbContext.Requisitions
                                                             join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                             join template in labDbContext.LabReportTemplates on requisition.ReportTemplateId equals template.ReportTemplateID
                                                             where requisition.PatientId == grp.Key.patient.PatientId
                                                            && requisition.SampleCode == grp.Key.SampleCode
                                                            && requisition.VisitType == grp.Key.VisitType
                                                            && requisition.WardName == grp.Key.WardName
                                                            && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                            && requisition.RunNumberType == grp.Key.RunNumberType
                                                            && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                            && requisition.OrderStatus.ToLower() == "result-added"
                                                            && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                            && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                             // group new { requisition }   by new { requisition, test } into g
                                                             select new LabPendingResultVM.LabTestDetail
                                                             {
                                                                 RequisitionId = requisition.RequisitionId,
                                                                 LabTestId = requisition.LabTestId,
                                                                 TestName = requisition.LabTestName

                                                             }).Distinct().ToList()
                                                }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);


                    var pendingHtmlNCS = (from req in labDbContext.Requisitions
                                          join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                          join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                          join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                          where req.OrderStatus.ToLower() == "result-added"
                                          && req.BillingStatus.ToLower() != "cancel" && (req.BillingStatus.ToLower() != "returned")
                                          && (template.TemplateType.ToLower() == "html")
                                          group new { req, template, patient, test } by new
                                          {
                                              patient,
                                              req,
                                              req.RunNumberType,
                                              req.BarCodeNumber,
                                              req.WardName
                                          } into grp
                                          select new LabPendingResultVM
                                          {
                                              PatientId = grp.Key.patient.PatientId,
                                              PatientCode = grp.Key.patient.PatientCode,
                                              DateOfBirth = grp.Key.patient.DateOfBirth,
                                              PhoneNumber = grp.Key.patient.PhoneNumber,
                                              Gender = grp.Key.patient.Gender,
                                              PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                              SampleCode = grp.Key.req.SampleCode,
                                              SampleDate = DbFunctions.TruncateTime(grp.Key.req.SampleCreatedOn).Value,
                                              VisitType = grp.Key.req.VisitType,
                                              RunNumType = grp.Key.RunNumberType,
                                              BarCodeNumber = grp.Key.BarCodeNumber,
                                              WardName = grp.Key.WardName,
                                              Tests = (from requisition in labDbContext.Requisitions
                                                       join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                       where requisition.PatientId == grp.Key.patient.PatientId
                                                        && requisition.RequisitionId == grp.Key.req.RequisitionId
                                                        && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                        && requisition.WardName == grp.Key.WardName
                                                        && requisition.RunNumberType == grp.Key.RunNumberType
                                                        && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                        && requisition.OrderStatus.ToLower() == "result-added"
                                                       //group requisition by new { test } into g
                                                       select new LabPendingResultVM.LabTestDetail
                                                       {
                                                           RequisitionId = requisition.RequisitionId,
                                                           LabTestId = requisition.LabTestId,
                                                           TestName = requisition.LabTestName
                                                           //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                           //LabTestId = g.Key.test.LabTestId,
                                                           //TestName = g.Key.test.LabTestName
                                                       }).ToList()
                                          }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

                    foreach (var rep in pendingHtmlNCS)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        results.Add(rep);
                    }
                    foreach (var repNormal in pendingNormalReports)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        results.Add(repNormal);
                    }

                    responseData.Results = results.OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

                }

                else if (reqType == "final-reports")
                {

                    var finalReportsProv = (from req in labDbContext.Requisitions
                                            join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                            join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                            join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                            where req.OrderStatus == "report-generated"
                                            && req.BillingStatus == "provisional"
                                            && (DbFunctions.TruncateTime(req.CreatedOn) >= FromDate && DbFunctions.TruncateTime(req.CreatedOn) <= ToDate)
                                            group new { req, patient, test } by new
                                            {
                                                patient,
                                                req.SampleCode,
                                                req.LabReportId,
                                                DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                req.VisitType,
                                                req.RunNumberType,
                                                report.IsPrinted,
                                                req.BarCodeNumber,
                                                req.WardName
                                            } into grp
                                            select new LabPendingResultVM
                                            {
                                                PatientId = grp.Key.patient.PatientId,
                                                PatientCode = grp.Key.patient.PatientCode,
                                                DateOfBirth = grp.Key.patient.DateOfBirth,
                                                PhoneNumber = grp.Key.patient.PhoneNumber,
                                                Gender = grp.Key.patient.Gender,
                                                PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                SampleCode = grp.Key.SampleCode,
                                                SampleDate = grp.Key.Value,
                                                SampleCodeFormatted = "",
                                                VisitType = grp.Key.VisitType,
                                                RunNumType = grp.Key.RunNumberType,
                                                IsPrinted = grp.Key.IsPrinted,
                                                BillingStatus = "provisional",
                                                BarCodeNumber = grp.Key.BarCodeNumber,
                                                ReportId = grp.Key.LabReportId,
                                                WardName = grp.Key.WardName,
                                                ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                     join employee in labDbContext.Employee
                                                                     on labRep.CreatedBy equals employee.EmployeeId
                                                                     where labRep.LabReportId == grp.Key.LabReportId
                                                                     select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),


                                                Tests = (from requisition in labDbContext.Requisitions
                                                         join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                         where requisition.PatientId == grp.Key.patient.PatientId
                                                         && requisition.SampleCode == grp.Key.SampleCode
                                                         && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                         && requisition.OrderStatus == "report-generated"
                                                         && requisition.LabReportId == grp.Key.LabReportId
                                                         && requisition.VisitType == grp.Key.VisitType
                                                         && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                         && requisition.WardName == grp.Key.WardName
                                                         && requisition.BillingStatus == "provisional"

                                                         select new LabPendingResultVM.LabTestDetail
                                                         {
                                                             RequisitionId = requisition.RequisitionId,
                                                             LabTestId = requisition.LabTestId,
                                                             TestName = requisition.LabTestName
                                                         }).ToList()
                                            }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();


                    var finalReportsPaidUnpaid = (from req in labDbContext.Requisitions
                                                  join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus == "report-generated"
                                                  && (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid")
                                                 && (DbFunctions.TruncateTime(req.CreatedOn) >= FromDate && DbFunctions.TruncateTime(req.CreatedOn) <= ToDate)
                                                  group new { req, patient, test } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      req.LabReportId,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RunNumberType,
                                                      report.IsPrinted,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      SampleCodeFormatted = "",
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      IsPrinted = grp.Key.IsPrinted,
                                                      BillingStatus = "paid",
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      ReportId = grp.Key.LabReportId,
                                                      WardName = grp.Key.WardName,
                                                      ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                           join employee in labDbContext.Employee
                                                                           on labRep.CreatedBy equals employee.EmployeeId
                                                                           where labRep.LabReportId == grp.Key.LabReportId
                                                                           select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                      Tests = (from requisition in labDbContext.Requisitions
                                                               join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                               where requisition.PatientId == grp.Key.patient.PatientId
                                                               && requisition.SampleCode == grp.Key.SampleCode
                                                               && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                               && requisition.OrderStatus == "report-generated"
                                                               && requisition.LabReportId == grp.Key.LabReportId
                                                               && requisition.VisitType == grp.Key.VisitType
                                                               && (requisition.BillingStatus == "paid" || requisition.BillingStatus == "unpaid")
                                                               && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                               select new LabPendingResultVM.LabTestDetail
                                                               {
                                                                   RequisitionId = requisition.RequisitionId,
                                                                   LabTestId = requisition.LabTestId,
                                                                   TestName = requisition.LabTestName
                                                               }).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();


                    var finalReports = finalReportsProv.Union(finalReportsPaidUnpaid);
                    finalReports = finalReports.OrderByDescending(rep => rep.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();


                    foreach (var rep in finalReports)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                    }
                    responseData.Results = finalReports;
                }

                else if (reqType == "allLabDataFromBarCodeNumber")
                {
                    LabMasterModel LabMasterData = new LabMasterModel();

                    int BarCodeNumber = barCodeNumber;

                    var firstReq = (from reqsn in labDbContext.Requisitions
                                    join patient in labDbContext.Patients on reqsn.PatientId equals patient.PatientId
                                    where reqsn.BarCodeNumber == BarCodeNumber
                                    select new
                                    {
                                        PatientId = patient.PatientId,
                                        Gender = patient.Gender,
                                        PatientCode = patient.PatientCode,
                                        PatientDob = patient.DateOfBirth,
                                        FirstName = patient.FirstName,
                                        MiddleName = patient.MiddleName,
                                        LastName = patient.LastName,
                                        SampleCreatedOn = reqsn.SampleCreatedOn
                                    }).FirstOrDefault();

                    LabMasterData.PatientId = firstReq.PatientId;
                    LabMasterData.PatientName = firstReq.FirstName + " " + (string.IsNullOrEmpty(firstReq.MiddleName) ? "" : firstReq.MiddleName + " ") + firstReq.LastName;
                    LabMasterData.Gender = firstReq.Gender;
                    LabMasterData.PatientCode = firstReq.PatientCode;
                    LabMasterData.DateOfBirth = firstReq.PatientDob;
                    LabMasterData.BarCodeNumber = BarCodeNumber;
                    LabMasterData.SampleCollectedOn = firstReq.SampleCreatedOn;

                    //Anish 24 Nov: This function below has single function for both getting AllLabData from BarcodeNumber or RunNumber-
                    //but is slow due large number of for loop containing if-else statement inside it
                    //LabMasterData = AllLabDataFromRunNumOrBarCode(labDbContext, BarCodeNumber);


                    //All PendingLabResults (for Add-Result page) of Particular Barcode Number
                    var reportWithHtmlTemplate = (from req in labDbContext.Requisitions
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus.ToLower() == "pending"
                                                  && req.SampleCode != null
                                                  && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && req.BarCodeNumber == BarCodeNumber
                                                  //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                  && (template.TemplateType.ToLower() == "html")
                                                  //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                  group new { req, test, template } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RequisitionId,
                                                      req.RunNumberType,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      Tests = grp.Select(a =>
                                                      new LabPendingResultVM.LabTestDetail()
                                                      {
                                                          RequisitionId = a.req.RequisitionId,
                                                          TestName = a.test.LabTestName,
                                                          LabTestId = a.test.LabTestId,
                                                          ReportTemplateId = a.template.ReportTemplateID,
                                                          ReportTemplateShortName = a.template.ReportTemplateShortName,
                                                          RunNumberType = a.req.RunNumberType
                                                      }).OrderBy(req => req.RequisitionId).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();

                    var reportWithNormalEntry = (from req in labDbContext.Requisitions
                                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                 join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                 where req.OrderStatus.ToLower() == "pending"
                                                 && req.SampleCode != null
                                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                 && req.BarCodeNumber == BarCodeNumber
                                                 //Removed as all can add result but cannot Print Report Until Bill is Paid (incase of OP)
                                                 //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                 && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                 //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                 group new { req, test, template } by new
                                                 {
                                                     patient,
                                                     req.SampleCode,
                                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                     req.VisitType,
                                                     req.RunNumberType,
                                                     req.BarCodeNumber,
                                                     req.WardName
                                                 } into grp
                                                 select new LabPendingResultVM
                                                 {
                                                     PatientId = grp.Key.patient.PatientId,
                                                     PatientCode = grp.Key.patient.PatientCode,
                                                     DateOfBirth = grp.Key.patient.DateOfBirth,
                                                     PhoneNumber = grp.Key.patient.PhoneNumber,
                                                     Gender = grp.Key.patient.Gender,
                                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                     SampleCode = grp.Key.SampleCode,
                                                     SampleDate = grp.Key.Value,
                                                     VisitType = grp.Key.VisitType,
                                                     RunNumType = grp.Key.RunNumberType,
                                                     BarCodeNumber = grp.Key.BarCodeNumber,
                                                     WardName = grp.Key.WardName,
                                                     Tests = grp.Select(a =>
                                                     new LabPendingResultVM.LabTestDetail()
                                                     {
                                                         RequisitionId = a.req.RequisitionId,
                                                         TestName = a.test.LabTestName,
                                                         LabTestId = a.test.LabTestId,
                                                         ReportTemplateId = a.template.ReportTemplateID,
                                                         ReportTemplateShortName = a.template.ReportTemplateShortName
                                                     }).OrderBy(req => req.RequisitionId).ToList()
                                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



                    foreach (var rep in reportWithHtmlTemplate)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        LabMasterData.AddResult.Add(rep);
                    }
                    foreach (var repNormal in reportWithNormalEntry)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        LabMasterData.AddResult.Add(repNormal);
                    }

                    LabMasterData.AddResult = LabMasterData.AddResult.OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



                    var verificationParameter = (from param in labDbContext.AdminParameters
                                                 where param.ParameterGroupName.ToLower() == "lab" && param.ParameterName == "LabReportVerificationNeededB4Print"
                                                 select param.ParameterValue).FirstOrDefault();

                    bool verificationRequired = Convert.ToBoolean(verificationParameter);

                    var pendingNormalReports = (from req in labDbContext.Requisitions
                                                join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                                && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                && req.BarCodeNumber == BarCodeNumber
                                                && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                group new { req, template, patient, test } by new
                                                {
                                                    patient,
                                                    req.SampleCode,
                                                    DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                    req.VisitType,
                                                    req.RunNumberType,
                                                    req.BarCodeNumber,
                                                    req.WardName
                                                } into grp
                                                select new LabPendingResultVM
                                                {
                                                    PatientId = grp.Key.patient.PatientId,
                                                    PatientCode = grp.Key.patient.PatientCode,
                                                    DateOfBirth = grp.Key.patient.DateOfBirth,
                                                    PhoneNumber = grp.Key.patient.PhoneNumber,
                                                    Gender = grp.Key.patient.Gender,
                                                    PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                    SampleCode = grp.Key.SampleCode,
                                                    SampleDate = grp.Key.Value,
                                                    VisitType = grp.Key.VisitType,
                                                    RunNumType = grp.Key.RunNumberType,
                                                    BarCodeNumber = grp.Key.BarCodeNumber,
                                                    WardName = grp.Key.WardName,
                                                    Tests = (from requisition in labDbContext.Requisitions
                                                             join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                             join template in labDbContext.LabReportTemplates on requisition.ReportTemplateId equals template.ReportTemplateID
                                                             where requisition.PatientId == grp.Key.patient.PatientId
                                                            && requisition.SampleCode == grp.Key.SampleCode
                                                            && requisition.VisitType == grp.Key.VisitType
                                                            && requisition.WardName == grp.Key.WardName
                                                            && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                            && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                            && requisition.OrderStatus.ToLower() == "result-added"
                                                            && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                            && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                             // group new { requisition }   by new { requisition, test } into g
                                                             select new LabPendingResultVM.LabTestDetail
                                                             {
                                                                 RequisitionId = requisition.RequisitionId,
                                                                 LabTestId = requisition.LabTestId,
                                                                 TestName = requisition.LabTestName

                                                             }).Distinct().ToList()
                                                }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);


                    var pendingHtmlNCS = (from req in labDbContext.Requisitions
                                          join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                          join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                          join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                          where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                          && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                          && req.BarCodeNumber == BarCodeNumber
                                          && (template.TemplateType.ToLower() == "html")
                                          group new { req, template, patient, test } by new
                                          {
                                              patient,
                                              req,
                                              req.RunNumberType,
                                              req.BarCodeNumber,
                                              req.WardName
                                          } into grp
                                          select new LabPendingResultVM
                                          {
                                              PatientId = grp.Key.patient.PatientId,
                                              PatientCode = grp.Key.patient.PatientCode,
                                              DateOfBirth = grp.Key.patient.DateOfBirth,
                                              PhoneNumber = grp.Key.patient.PhoneNumber,
                                              Gender = grp.Key.patient.Gender,
                                              PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                              SampleCode = grp.Key.req.SampleCode,
                                              SampleDate = DbFunctions.TruncateTime(grp.Key.req.SampleCreatedOn).Value,
                                              VisitType = grp.Key.req.VisitType,
                                              RunNumType = grp.Key.RunNumberType,
                                              BarCodeNumber = grp.Key.BarCodeNumber,
                                              WardName = grp.Key.WardName,
                                              Tests = (from requisition in labDbContext.Requisitions
                                                       join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                       where requisition.PatientId == grp.Key.patient.PatientId
                                                       && requisition.BarCodeNumber == grp.Key.req.BarCodeNumber
                                                       && requisition.WardName == grp.Key.req.WardName
                                                       && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                       && requisition.OrderStatus.ToLower() == "result-added"
                                                       && requisition.RequisitionId == grp.Key.req.RequisitionId
                                                       //group requisition by new { test } into g
                                                       select new LabPendingResultVM.LabTestDetail
                                                       {
                                                           RequisitionId = requisition.RequisitionId,
                                                           LabTestId = requisition.LabTestId,
                                                           TestName = requisition.LabTestName
                                                           //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                           //LabTestId = g.Key.test.LabTestId,
                                                           //TestName = g.Key.test.LabTestName
                                                       }).ToList()
                                          }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

                    foreach (var rep in pendingHtmlNCS)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        LabMasterData.PendingReport.Add(rep);
                    }
                    foreach (var repNormal in pendingNormalReports)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        LabMasterData.PendingReport.Add(repNormal);
                    }

                    var finalReportsProv = (from req in labDbContext.Requisitions
                                            join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                            join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                            join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                            where req.OrderStatus == "report-generated"
                                            && req.BillingStatus == "provisional"
                                            //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                            && req.BarCodeNumber == BarCodeNumber
                                            group new { req, patient, test } by new
                                            {
                                                patient,
                                                req.SampleCode,
                                                req.LabReportId,
                                                DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                req.VisitType,
                                                req.RunNumberType,
                                                report.IsPrinted,
                                                req.BarCodeNumber,
                                                req.WardName
                                            } into grp
                                            select new LabPendingResultVM
                                            {
                                                PatientId = grp.Key.patient.PatientId,
                                                PatientCode = grp.Key.patient.PatientCode,
                                                DateOfBirth = grp.Key.patient.DateOfBirth,
                                                PhoneNumber = grp.Key.patient.PhoneNumber,
                                                Gender = grp.Key.patient.Gender,
                                                PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                SampleCode = grp.Key.SampleCode,
                                                SampleDate = grp.Key.Value,
                                                SampleCodeFormatted = "",
                                                VisitType = grp.Key.VisitType,
                                                RunNumType = grp.Key.RunNumberType,
                                                IsPrinted = grp.Key.IsPrinted,
                                                BillingStatus = "provisional",
                                                BarCodeNumber = grp.Key.BarCodeNumber,
                                                WardName = grp.Key.WardName,
                                                ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                     join employee in labDbContext.Employee
                                                                     on labRep.CreatedBy equals employee.EmployeeId
                                                                     where labRep.LabReportId == grp.Key.LabReportId
                                                                     select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                Tests = (from requisition in labDbContext.Requisitions
                                                         join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                         where requisition.PatientId == grp.Key.patient.PatientId
                                                         && requisition.SampleCode == grp.Key.SampleCode
                                                         && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                         && requisition.OrderStatus == "report-generated"
                                                         && requisition.LabReportId == grp.Key.LabReportId
                                                         && requisition.RunNumberType == grp.Key.RunNumberType
                                                         && requisition.VisitType == grp.Key.VisitType
                                                         && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                         && requisition.WardName == grp.Key.WardName
                                                         && requisition.BillingStatus == "provisional"

                                                         //group requisition by new { test } into g
                                                         select new LabPendingResultVM.LabTestDetail
                                                         {
                                                             RequisitionId = requisition.RequisitionId,
                                                             LabTestId = requisition.LabTestId,
                                                             TestName = requisition.LabTestName
                                                             //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                             //LabTestId = g.Key.test.LabTestId,
                                                             //TestName = g.Key.test.LabTestName
                                                         }).ToList()
                                            }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();


                    var finalReportsPaidUnpaid = (from req in labDbContext.Requisitions
                                                  join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus == "report-generated"
                                                  && (req.BillingStatus == "paid" || req.BillingStatus == "unpaid")
                                                  //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && req.BarCodeNumber == BarCodeNumber
                                                  group new { req, patient, test } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      req.LabReportId,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RunNumberType,
                                                      report.IsPrinted,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      SampleCodeFormatted = "",
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      IsPrinted = grp.Key.IsPrinted,
                                                      BillingStatus = "paid",
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                           join employee in labDbContext.Employee
                                                                           on labRep.CreatedBy equals employee.EmployeeId
                                                                           where labRep.LabReportId == grp.Key.LabReportId
                                                                           select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                      Tests = (from requisition in labDbContext.Requisitions
                                                               join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                               where requisition.PatientId == grp.Key.patient.PatientId
                                                               && requisition.SampleCode == grp.Key.SampleCode
                                                               && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                               && requisition.OrderStatus == "report-generated"
                                                               && requisition.LabReportId == grp.Key.LabReportId
                                                               && requisition.RunNumberType == grp.Key.RunNumberType
                                                               && requisition.VisitType == grp.Key.VisitType
                                                               && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                               && requisition.WardName == grp.Key.WardName
                                                               && (requisition.BillingStatus == "paid" || requisition.BillingStatus == "unpaid")

                                                               //group requisition by new { test } into g
                                                               select new LabPendingResultVM.LabTestDetail
                                                               {
                                                                   RequisitionId = requisition.RequisitionId,
                                                                   LabTestId = requisition.LabTestId,
                                                                   TestName = requisition.LabTestName
                                                                   //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                                   //LabTestId = g.Key.test.LabTestId,
                                                                   //TestName = g.Key.test.LabTestName
                                                               }).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();



                    var finalReports = finalReportsProv.Union(finalReportsPaidUnpaid);
                    finalReports = finalReports.OrderByDescending(rep => rep.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();
                    List<LabPendingResultVM> finalReportList = new List<LabPendingResultVM>(finalReports);

                    LabMasterData.FinalReport = finalReportList;

                    var parameterOutPatWithProvisional = (from coreData in labDbContext.AdminParameters
                                                          where coreData.ParameterGroupName.ToLower() == "lab"
                                                          && coreData.ParameterName == "AllowLabReportToPrintOnProvisional"
                                                          select coreData.ParameterValue).FirstOrDefault();

                    bool allowOutPatWithProv = false;

                    if (!String.IsNullOrEmpty(parameterOutPatWithProvisional) && parameterOutPatWithProvisional.ToLower() == "true")
                    {
                        allowOutPatWithProv = true;
                    }


                    foreach (var rep in LabMasterData.FinalReport)
                    {
                        if (!String.IsNullOrEmpty(rep.VisitType) && !String.IsNullOrEmpty(rep.BillingStatus))
                        {
                            rep.IsValidToPrint = ValidatePrintOption(allowOutPatWithProv, rep.VisitType, rep.BillingStatus);
                        }
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                    }

                    responseData.Results = LabMasterData;

                }


                else if (reqType == "allLabDataFromRunNumber")
                {
                    LabMasterModel LabMasterData = new LabMasterModel();
                    string completeSampleCode = formattedSampleCode;


                    //LabMasterData = AllLabDataFromRunNumOrBarCode(labDbContext, formattedCode:completeSampleCode);

                    var mainCode = formattedSampleCode.Split('/');
                    int samplNumber = Convert.ToInt32(mainCode[0]);
                    int code = Convert.ToInt32(mainCode[1]);

                    DateTime englishDateToday = DateTime.Now;
                    NepaliDateType nepaliDate = DanpheDateConvertor.ConvertEngToNepDate(englishDateToday);

                    if (code > 32)
                    {
                        nepaliDate.Year = 2000 + code;
                    }
                    else
                    {
                        nepaliDate.Day = code;
                    }

                    englishDateToday = DanpheDateConvertor.ConvertNepToEngDate(nepaliDate);

                    var reportWithHtmlTemplate = (from req in labDbContext.Requisitions
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus.ToLower() == "pending"
                                                  && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                                  && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                                  && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && (template.TemplateType.ToLower() == "html")
                                                  group new { req, test, template } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RequisitionId,
                                                      req.RunNumberType,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      Tests = grp.Select(a =>
                                                      new LabPendingResultVM.LabTestDetail()
                                                      {
                                                          RequisitionId = a.req.RequisitionId,
                                                          TestName = a.test.LabTestName,
                                                          LabTestId = a.test.LabTestId,
                                                          ReportTemplateId = a.template.ReportTemplateID,
                                                          ReportTemplateShortName = a.template.ReportTemplateShortName,
                                                          RunNumberType = a.req.RunNumberType
                                                      }).OrderBy(req => req.RequisitionId).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();

                    var reportWithNormalEntry = (from req in labDbContext.Requisitions
                                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                 join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                 where req.OrderStatus.ToLower() == "pending"
                                                 && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                                 && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                 && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                 group new { req, test, template } by new
                                                 {
                                                     patient,
                                                     req.SampleCode,
                                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                     req.VisitType,
                                                     req.RunNumberType,
                                                     req.BarCodeNumber,
                                                     req.WardName
                                                 } into grp
                                                 select new LabPendingResultVM
                                                 {
                                                     PatientId = grp.Key.patient.PatientId,
                                                     PatientCode = grp.Key.patient.PatientCode,
                                                     DateOfBirth = grp.Key.patient.DateOfBirth,
                                                     PhoneNumber = grp.Key.patient.PhoneNumber,
                                                     Gender = grp.Key.patient.Gender,
                                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                     SampleCode = grp.Key.SampleCode,
                                                     SampleDate = grp.Key.Value,
                                                     VisitType = grp.Key.VisitType,
                                                     RunNumType = grp.Key.RunNumberType,
                                                     BarCodeNumber = grp.Key.BarCodeNumber,
                                                     WardName = grp.Key.WardName,
                                                     Tests = grp.Select(a =>
                                                     new LabPendingResultVM.LabTestDetail()
                                                     {
                                                         RequisitionId = a.req.RequisitionId,
                                                         TestName = a.test.LabTestName,
                                                         LabTestId = a.test.LabTestId,
                                                         ReportTemplateId = a.template.ReportTemplateID,
                                                         ReportTemplateShortName = a.template.ReportTemplateShortName
                                                     }).OrderBy(req => req.RequisitionId).ToList()
                                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



                    foreach (var rep in reportWithHtmlTemplate)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        if (rep.SampleCodeFormatted == completeSampleCode)
                        {
                            LabMasterData.AddResult.Add(rep);
                        }

                    }
                    foreach (var repNormal in reportWithNormalEntry)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        if (repNormal.SampleCodeFormatted == completeSampleCode)
                        {
                            LabMasterData.AddResult.Add(repNormal);
                        }
                    }

                    LabMasterData.AddResult = LabMasterData.AddResult.OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();


                    var verificationParameter = (from param in labDbContext.AdminParameters
                                                 where param.ParameterGroupName.ToLower() == "lab" && param.ParameterName == "LabReportVerificationNeededB4Print"
                                                 select param.ParameterValue).FirstOrDefault();

                    bool verificationRequired = Convert.ToBoolean(verificationParameter);

                    var pendingNormalReports = (from req in labDbContext.Requisitions
                                                join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                                && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                                && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                                && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                group new { req, template, patient, test } by new
                                                {
                                                    patient,
                                                    req.SampleCode,
                                                    DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                    req.VisitType,
                                                    req.RunNumberType,
                                                    req.BarCodeNumber,
                                                    req.WardName
                                                } into grp
                                                select new LabPendingResultVM
                                                {
                                                    PatientId = grp.Key.patient.PatientId,
                                                    PatientCode = grp.Key.patient.PatientCode,
                                                    DateOfBirth = grp.Key.patient.DateOfBirth,
                                                    PhoneNumber = grp.Key.patient.PhoneNumber,
                                                    Gender = grp.Key.patient.Gender,
                                                    PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                    SampleCode = grp.Key.SampleCode,
                                                    SampleDate = grp.Key.Value,
                                                    VisitType = grp.Key.VisitType,
                                                    RunNumType = grp.Key.RunNumberType,
                                                    BarCodeNumber = grp.Key.BarCodeNumber,
                                                    WardName = grp.Key.WardName,
                                                    Tests = (from requisition in labDbContext.Requisitions
                                                             join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                             join template in labDbContext.LabReportTemplates on requisition.ReportTemplateId equals template.ReportTemplateID
                                                             where requisition.PatientId == grp.Key.patient.PatientId
                                                            && requisition.SampleCode == grp.Key.SampleCode
                                                            && requisition.RunNumberType == grp.Key.RunNumberType
                                                            && requisition.VisitType == grp.Key.VisitType
                                                            && requisition.WardName == grp.Key.WardName
                                                            && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                            && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                            && requisition.OrderStatus.ToLower() == "result-added"
                                                            && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                            && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                             select new LabPendingResultVM.LabTestDetail
                                                             {
                                                                 RequisitionId = requisition.RequisitionId,
                                                                 LabTestId = requisition.LabTestId,
                                                                 TestName = requisition.LabTestName

                                                             }).Distinct().ToList()
                                                }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);


                    var pendingHtmlNCS = (from req in labDbContext.Requisitions
                                          join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                          join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                          join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                          where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                          && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                          && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                         && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                          && (template.TemplateType.ToLower() == "html")
                                          group new { req, template, patient, test } by new
                                          {
                                              patient,
                                              req,
                                              req.RunNumberType,
                                              req.BarCodeNumber,
                                              req.WardName
                                          } into grp
                                          select new LabPendingResultVM
                                          {
                                              PatientId = grp.Key.patient.PatientId,
                                              PatientCode = grp.Key.patient.PatientCode,
                                              DateOfBirth = grp.Key.patient.DateOfBirth,
                                              PhoneNumber = grp.Key.patient.PhoneNumber,
                                              Gender = grp.Key.patient.Gender,
                                              PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                              SampleCode = grp.Key.req.SampleCode,
                                              SampleDate = DbFunctions.TruncateTime(grp.Key.req.SampleCreatedOn).Value,
                                              VisitType = grp.Key.req.VisitType,
                                              RunNumType = grp.Key.RunNumberType,
                                              BarCodeNumber = grp.Key.BarCodeNumber,
                                              WardName = grp.Key.WardName,
                                              Tests = (from requisition in labDbContext.Requisitions
                                                       join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                       where requisition.PatientId == grp.Key.patient.PatientId
                                                       && requisition.BarCodeNumber == grp.Key.req.BarCodeNumber
                                                       && requisition.WardName == grp.Key.req.WardName
                                                       && requisition.RunNumberType == grp.Key.RunNumberType
                                                       && requisition.RequisitionId == grp.Key.req.RequisitionId
                                                       select new LabPendingResultVM.LabTestDetail
                                                       {
                                                           RequisitionId = requisition.RequisitionId,
                                                           LabTestId = requisition.LabTestId,
                                                           TestName = requisition.LabTestName
                                                       }).ToList()
                                          }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

                    foreach (var rep in pendingHtmlNCS)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        if (rep.SampleCodeFormatted == completeSampleCode)
                        {
                            LabMasterData.PendingReport.Add(rep);
                        }
                    }
                    foreach (var repNormal in pendingNormalReports)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        if (repNormal.SampleCodeFormatted == completeSampleCode)
                        {
                            LabMasterData.PendingReport.Add(repNormal);
                        }
                    }

                    var finalReportsProv = (from req in labDbContext.Requisitions
                                            join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                            join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                            join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                            where req.OrderStatus == "report-generated"
                                            && req.BillingStatus.ToLower() == "provisional"
                                            //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                            && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                            && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                            group new { req, patient, test } by new
                                            {
                                                patient,
                                                req.SampleCode,
                                                req.LabReportId,
                                                DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                req.VisitType,
                                                req.RunNumberType,
                                                report.IsPrinted,
                                                req.BarCodeNumber,
                                                req.WardName
                                            } into grp
                                            select new LabPendingResultVM
                                            {
                                                PatientId = grp.Key.patient.PatientId,
                                                PatientCode = grp.Key.patient.PatientCode,
                                                DateOfBirth = grp.Key.patient.DateOfBirth,
                                                PhoneNumber = grp.Key.patient.PhoneNumber,
                                                Gender = grp.Key.patient.Gender,
                                                PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                SampleCode = grp.Key.SampleCode,
                                                SampleDate = grp.Key.Value,
                                                SampleCodeFormatted = "",
                                                VisitType = grp.Key.VisitType,
                                                RunNumType = grp.Key.RunNumberType,
                                                IsPrinted = grp.Key.IsPrinted,
                                                BillingStatus = "provisional",
                                                BarCodeNumber = grp.Key.BarCodeNumber,
                                                WardName = grp.Key.WardName,
                                                ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                     join employee in labDbContext.Employee
                                                                     on labRep.CreatedBy equals employee.EmployeeId
                                                                     where labRep.LabReportId == grp.Key.LabReportId
                                                                     select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                Tests = (from requisition in labDbContext.Requisitions
                                                         join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                         where requisition.PatientId == grp.Key.patient.PatientId
                                                         && requisition.SampleCode == grp.Key.SampleCode
                                                         && requisition.VisitType == grp.Key.VisitType
                                                         && requisition.WardName == grp.Key.WardName
                                                         && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                         && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                         && requisition.OrderStatus == "report-generated"
                                                         && requisition.LabReportId == grp.Key.LabReportId
                                                         && requisition.BillingStatus == "provisional"
                                                         && requisition.RunNumberType == grp.Key.RunNumberType

                                                         select new LabPendingResultVM.LabTestDetail
                                                         {
                                                             RequisitionId = requisition.RequisitionId,
                                                             LabTestId = requisition.LabTestId,
                                                             TestName = requisition.LabTestName
                                                         }).ToList()
                                            }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

                    var finalReportsPaidUnpaid = (from req in labDbContext.Requisitions
                                                  join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus == "report-generated"
                                                  && (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid")
                                                  //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false)
                                                  && (code > 32 ? (req.SampleCreatedOn.Value.Year == englishDateToday.Year) : true)
                                                  group new { req, patient, test } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      req.LabReportId,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RunNumberType,
                                                      report.IsPrinted,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      SampleCodeFormatted = "",
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      IsPrinted = grp.Key.IsPrinted,
                                                      BillingStatus = "paid",
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                           join employee in labDbContext.Employee
                                                                           on labRep.CreatedBy equals employee.EmployeeId
                                                                           where labRep.LabReportId == grp.Key.LabReportId
                                                                           select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                      Tests = (from requisition in labDbContext.Requisitions
                                                               join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                               where requisition.PatientId == grp.Key.patient.PatientId
                                                               && requisition.SampleCode == grp.Key.SampleCode
                                                               && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                               && requisition.OrderStatus == "report-generated"
                                                               && requisition.LabReportId == grp.Key.LabReportId
                                                               && requisition.RunNumberType == grp.Key.RunNumberType
                                                               && requisition.VisitType == grp.Key.VisitType
                                                               && requisition.WardName == grp.Key.WardName
                                                               && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                               && (requisition.BillingStatus == "paid" || requisition.BillingStatus == "unpaid")

                                                               select new LabPendingResultVM.LabTestDetail
                                                               {
                                                                   RequisitionId = requisition.RequisitionId,
                                                                   LabTestId = requisition.LabTestId,
                                                                   TestName = requisition.LabTestName
                                                               }).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

                    var finalReports = finalReportsProv.Union(finalReportsPaidUnpaid);
                    finalReports = finalReports.OrderByDescending(rep => rep.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

                    var parameterOutPatWithProvisional = (from coreData in labDbContext.AdminParameters
                                                          where coreData.ParameterGroupName.ToLower() == "lab"
                                                          && coreData.ParameterName == "AllowLabReportToPrintOnProvisional"
                                                          select coreData.ParameterValue).FirstOrDefault();

                    bool allowOutPatWithProv = false;

                    if (!String.IsNullOrEmpty(parameterOutPatWithProvisional) && parameterOutPatWithProvisional.ToLower() == "true")
                    {
                        allowOutPatWithProv = true;
                    }


                    foreach (var rep in finalReports)
                    {
                        if (!String.IsNullOrEmpty(rep.VisitType) && !String.IsNullOrEmpty(rep.BillingStatus))
                        {
                            rep.IsValidToPrint = ValidatePrintOption(allowOutPatWithProv, rep.VisitType, rep.BillingStatus);
                        }

                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        if (rep.SampleCodeFormatted == completeSampleCode)
                        {
                            LabMasterData.FinalReport.Add(rep);
                        }
                    }


                    responseData.Results = LabMasterData;

                }

                else if (reqType == "allLabDataFromPatientName")
                {
                    LabMasterModel LabMasterData = new LabMasterModel();

                    int PatientId = patientId;

                    var verificationParameter = (from param in labDbContext.AdminParameters
                                                 where param.ParameterGroupName.ToLower() == "lab" && param.ParameterName == "LabReportVerificationNeededB4Print"
                                                 select param.ParameterValue).FirstOrDefault();

                    bool verificationRequired = Convert.ToBoolean(verificationParameter);

                    //All LabRequisitions of Patient
                    var histoPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                         join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                         where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                         && req.PatientId == PatientId
                                         && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "histo")
                                         select new Requisition
                                         {
                                             RequisitionId = req.RequisitionId,
                                             PatientId = req.PatientId,
                                             PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                             PatientCode = req.Patient.PatientCode,
                                             DateOfBirth = req.Patient.DateOfBirth,
                                             Gender = req.Patient.Gender,
                                             PhoneNumber = req.Patient.PhoneNumber,
                                             LastestRequisitionDate = req.OrderDateTime,
                                             VisitType = req.VisitType,
                                             RunNumberType = req.RunNumberType,
                                             WardName = req.WardName
                                         }).OrderByDescending(a => a.LastestRequisitionDate).ToList(); ;

                    //Removed to show all detail regardless of BillingStatus
                    //&& (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                    var cytoPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                        join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                        where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                        && req.PatientId == PatientId
                                        && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "cyto")
                                        select new Requisition
                                        {
                                            RequisitionId = req.RequisitionId,
                                            PatientId = req.PatientId,
                                            PatientName = req.Patient.FirstName + " " + (string.IsNullOrEmpty(req.Patient.MiddleName) ? "" : req.Patient.MiddleName + " ") + req.Patient.LastName,
                                            PatientCode = req.Patient.PatientCode,
                                            DateOfBirth = req.Patient.DateOfBirth,
                                            Gender = req.Patient.Gender,
                                            PhoneNumber = req.Patient.PhoneNumber,
                                            LastestRequisitionDate = req.OrderDateTime,
                                            VisitType = req.VisitType,
                                            RunNumberType = req.RunNumberType,
                                            WardName = req.WardName
                                        }).OrderByDescending(a => a.LastestRequisitionDate).ToList();

                    //.OrderByDescending(a => a.LatestRequisitionDate).ToList()

                    //Removed to show all detail regardless of BillingStatus
                    //&& (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "unpaid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                    var normalPatients = (from req in labDbContext.Requisitions.Include("Patient")
                                          join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                          //show only paid and unpaid requisitions in the list.
                                          //show only IsActive=True and IsActive=NULL requests, Hide IsActive=False. -- sud: 15Sept'18
                                          //if IsActive has value then it should be true, if it's null then its true by default. 
                                          where ((req.IsActive.HasValue ? req.IsActive.Value == true : true) && req.OrderStatus.ToLower() == "active"
                                          && req.PatientId == PatientId
                                          && (req.BillingStatus.ToLower() != "cancel") && (req.BillingStatus.ToLower() != "returned") && req.RunNumberType.ToLower() == "normal")
                                          group req by new { req.Patient, req.VisitType, req.WardName } into p
                                          select new Requisition
                                          {
                                              RequisitionId = (long)0,
                                              PatientId = p.Key.Patient.PatientId,
                                              PatientName = p.Key.Patient.FirstName + " " + (string.IsNullOrEmpty(p.Key.Patient.MiddleName) ? "" : p.Key.Patient.MiddleName + " ") + p.Key.Patient.LastName,
                                              PatientCode = p.Key.Patient.PatientCode,
                                              DateOfBirth = p.Key.Patient.DateOfBirth,
                                              Gender = p.Key.Patient.Gender,
                                              PhoneNumber = p.Key.Patient.PhoneNumber,
                                              LastestRequisitionDate = p.Max(r => r.OrderDateTime),
                                              VisitType = p.Key.VisitType,
                                              RunNumberType = "normal",
                                              WardName = p.Key.WardName
                                              //IsAdmitted = (from adm in labDbContext.Admissions
                                              //              where adm.PatientId == p.Key.Patient.PatientId && adm.AdmissionStatus == "admitted"
                                              //              select adm.AdmissionStatus).FirstOrDefault() == null ? true : false
                                          }).OrderByDescending(b => b.LastestRequisitionDate).ToList();


                    var combined = histoPatients.Union(cytoPatients).Union(normalPatients);
                    var allReqs = combined.OrderByDescending(c => c.LastestRequisitionDate);
                    List<Requisition> allRequisitionsOfPat = new List<Requisition>(allReqs);
                    LabMasterData.LabRequisitions = allRequisitionsOfPat;



                    //All PendingLabResults (for Add-Result page) of Particular Barcode Number
                    var reportWithHtmlTemplate = (from req in labDbContext.Requisitions
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus.ToLower() == "pending"
                                                  && req.SampleCode != null
                                                  && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && req.PatientId == PatientId
                                                  //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                  && (template.TemplateType.ToLower() == "html")
                                                  //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                  group new { req, test, template } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RequisitionId,
                                                      req.RunNumberType,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      Tests = grp.Select(a =>
                                                      new LabPendingResultVM.LabTestDetail()
                                                      {
                                                          RequisitionId = a.req.RequisitionId,
                                                          TestName = a.test.LabTestName,
                                                          LabTestId = a.test.LabTestId,
                                                          ReportTemplateId = a.template.ReportTemplateID,
                                                          ReportTemplateShortName = a.template.ReportTemplateShortName,
                                                          RunNumberType = a.req.RunNumberType
                                                      }).OrderBy(req => req.RequisitionId).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();

                    var reportWithNormalEntry = (from req in labDbContext.Requisitions
                                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                 join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                 where req.OrderStatus.ToLower() == "pending"
                                                 && req.SampleCode != null
                                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                 && req.PatientId == PatientId
                                                 //Removed as all can add result but cannot Print Report Until Bill is Paid (incase of OP)
                                                 //&& (req.BillingStatus == "paid" || (req.BillingStatus == "provisional" && req.VisitType == "inpatient"))
                                                 && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                 //ashim: 01Sep2018 : updated group by logic: we're now grouping by samplecode and patient
                                                 group new { req, test, template } by new
                                                 {
                                                     patient,
                                                     req.SampleCode,
                                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                     req.VisitType,
                                                     req.RunNumberType,
                                                     req.BarCodeNumber,
                                                     req.WardName
                                                 } into grp
                                                 select new LabPendingResultVM
                                                 {
                                                     PatientId = grp.Key.patient.PatientId,
                                                     PatientCode = grp.Key.patient.PatientCode,
                                                     DateOfBirth = grp.Key.patient.DateOfBirth,
                                                     PhoneNumber = grp.Key.patient.PhoneNumber,
                                                     Gender = grp.Key.patient.Gender,
                                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                     SampleCode = grp.Key.SampleCode,
                                                     SampleDate = grp.Key.Value,
                                                     VisitType = grp.Key.VisitType,
                                                     RunNumType = grp.Key.RunNumberType,
                                                     BarCodeNumber = grp.Key.BarCodeNumber,
                                                     WardName = grp.Key.WardName,
                                                     Tests = grp.Select(a =>
                                                     new LabPendingResultVM.LabTestDetail()
                                                     {
                                                         RequisitionId = a.req.RequisitionId,
                                                         TestName = a.test.LabTestName,
                                                         LabTestId = a.test.LabTestId,
                                                         ReportTemplateId = a.template.ReportTemplateID,
                                                         ReportTemplateShortName = a.template.ReportTemplateShortName
                                                     }).OrderBy(req => req.RequisitionId).ToList()
                                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



                    foreach (var rep in reportWithHtmlTemplate)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        LabMasterData.AddResult.Add(rep);
                    }
                    foreach (var repNormal in reportWithNormalEntry)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        LabMasterData.AddResult.Add(repNormal);
                    }

                    LabMasterData.AddResult = LabMasterData.AddResult.OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();


                    var pendingNormalReports = (from req in labDbContext.Requisitions
                                                join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                                join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                                && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                && req.PatientId == PatientId
                                                && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                group new { req, template, patient, test } by new
                                                {
                                                    patient,
                                                    req.SampleCode,
                                                    DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                    req.VisitType,
                                                    req.RunNumberType,
                                                    req.BarCodeNumber,
                                                    req.WardName
                                                } into grp
                                                select new LabPendingResultVM
                                                {
                                                    PatientId = grp.Key.patient.PatientId,
                                                    PatientCode = grp.Key.patient.PatientCode,
                                                    DateOfBirth = grp.Key.patient.DateOfBirth,
                                                    PhoneNumber = grp.Key.patient.PhoneNumber,
                                                    Gender = grp.Key.patient.Gender,
                                                    PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                    SampleCode = grp.Key.SampleCode,
                                                    SampleDate = grp.Key.Value,
                                                    VisitType = grp.Key.VisitType,
                                                    RunNumType = grp.Key.RunNumberType,
                                                    BarCodeNumber = grp.Key.BarCodeNumber,
                                                    WardName = grp.Key.WardName,
                                                    Tests = (from requisition in labDbContext.Requisitions
                                                             join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                             join template in labDbContext.LabReportTemplates on requisition.ReportTemplateId equals template.ReportTemplateID
                                                             where requisition.PatientId == PatientId
                                                            && requisition.SampleCode == grp.Key.SampleCode
                                                            && requisition.VisitType == grp.Key.VisitType
                                                            && requisition.WardName == grp.Key.WardName
                                                            && requisition.RunNumberType == grp.Key.RunNumberType
                                                            && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                            && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                            && requisition.OrderStatus.ToLower() == "result-added"
                                                            && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
                                                            && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
                                                             // group new { requisition }   by new { requisition, test } into g
                                                             select new LabPendingResultVM.LabTestDetail
                                                             {
                                                                 RequisitionId = requisition.RequisitionId,
                                                                 LabTestId = requisition.LabTestId,
                                                                 TestName = requisition.LabTestName

                                                             }).Distinct().ToList()
                                                }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);


                    var pendingHtmlNCS = (from req in labDbContext.Requisitions
                                          join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                          join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
                                          join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                          where (verificationRequired ? (req.OrderStatus.ToLower() == "result-added" && (req.IsVerified.HasValue ? req.IsVerified == false : true)) : req.OrderStatus.ToLower() == "result-added")
                                          && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                          && req.PatientId == PatientId
                                          && (template.TemplateType.ToLower() == "html")
                                          group new { req, template, patient, test } by new
                                          {
                                              patient,
                                              req,
                                              req.RunNumberType,
                                              req.BarCodeNumber,
                                              req.WardName
                                          } into grp
                                          select new LabPendingResultVM
                                          {
                                              PatientId = grp.Key.patient.PatientId,
                                              PatientCode = grp.Key.patient.PatientCode,
                                              DateOfBirth = grp.Key.patient.DateOfBirth,
                                              PhoneNumber = grp.Key.patient.PhoneNumber,
                                              Gender = grp.Key.patient.Gender,
                                              PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                              SampleCode = grp.Key.req.SampleCode,
                                              SampleDate = DbFunctions.TruncateTime(grp.Key.req.SampleCreatedOn).Value,
                                              VisitType = grp.Key.req.VisitType,
                                              RunNumType = grp.Key.RunNumberType,
                                              BarCodeNumber = grp.Key.BarCodeNumber,
                                              WardName = grp.Key.WardName,
                                              Tests = (from requisition in labDbContext.Requisitions
                                                       join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                       where requisition.PatientId == PatientId
                                                        && requisition.RequisitionId == grp.Key.req.RequisitionId
                                                        && requisition.BarCodeNumber == grp.Key.req.BarCodeNumber
                                                        && requisition.WardName == grp.Key.req.WardName
                                                        && requisition.RunNumberType == grp.Key.RunNumberType
                                                        && requisition.SampleCode == grp.Key.req.SampleCode
                                                       //group requisition by new { test } into g
                                                       select new LabPendingResultVM.LabTestDetail
                                                       {
                                                           RequisitionId = requisition.RequisitionId,
                                                           LabTestId = requisition.LabTestId,
                                                           TestName = requisition.LabTestName
                                                           //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                           //LabTestId = g.Key.test.LabTestId,
                                                           //TestName = g.Key.test.LabTestName
                                                       }).ToList()
                                          }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

                    foreach (var rep in pendingHtmlNCS)
                    {
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                        LabMasterData.PendingReport.Add(rep);
                    }
                    foreach (var repNormal in pendingNormalReports)
                    {
                        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
                        LabMasterData.PendingReport.Add(repNormal);
                    }



                    var finalReportsProv = (from req in labDbContext.Requisitions
                                            join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                            join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                            join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                            where req.OrderStatus == "report-generated"
                                            && req.BillingStatus.ToLower() == "provisional"
                                            //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                            && req.PatientId == PatientId
                                            group new { req, patient, test } by new
                                            {
                                                patient,
                                                req.SampleCode,
                                                req.LabReportId,
                                                DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                req.VisitType,
                                                req.RunNumberType,
                                                report.IsPrinted,
                                                req.BarCodeNumber,
                                                req.WardName
                                            } into grp
                                            select new LabPendingResultVM
                                            {
                                                PatientId = grp.Key.patient.PatientId,
                                                PatientCode = grp.Key.patient.PatientCode,
                                                DateOfBirth = grp.Key.patient.DateOfBirth,
                                                PhoneNumber = grp.Key.patient.PhoneNumber,
                                                Gender = grp.Key.patient.Gender,
                                                PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                SampleCode = grp.Key.SampleCode,
                                                SampleDate = grp.Key.Value,
                                                SampleCodeFormatted = "",
                                                VisitType = grp.Key.VisitType,
                                                RunNumType = grp.Key.RunNumberType,
                                                IsPrinted = grp.Key.IsPrinted,
                                                BillingStatus = "provisional",
                                                BarCodeNumber = grp.Key.BarCodeNumber,
                                                WardName = grp.Key.WardName,
                                                ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                     join employee in labDbContext.Employee
                                                                     on labRep.CreatedBy equals employee.EmployeeId
                                                                     where labRep.LabReportId == grp.Key.LabReportId
                                                                     select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                Tests = (from requisition in labDbContext.Requisitions
                                                         join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                         where requisition.PatientId == PatientId
                                                         && requisition.VisitType == grp.Key.VisitType
                                                         && requisition.WardName == grp.Key.WardName
                                                         && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                         && requisition.SampleCode == grp.Key.SampleCode
                                                         && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                         && requisition.OrderStatus == "report-generated"
                                                         && requisition.LabReportId == grp.Key.LabReportId
                                                         && requisition.RunNumberType == grp.Key.RunNumberType
                                                         && requisition.BillingStatus == "provisional"

                                                         //group requisition by new { test } into g
                                                         select new LabPendingResultVM.LabTestDetail
                                                         {
                                                             RequisitionId = requisition.RequisitionId,
                                                             LabTestId = requisition.LabTestId,
                                                             TestName = requisition.LabTestName
                                                             //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                             //LabTestId = g.Key.test.LabTestId,
                                                             //TestName = g.Key.test.LabTestName
                                                         }).ToList()
                                            }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

                    var finalReportsPaidUnpaid = (from req in labDbContext.Requisitions
                                                  join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
                                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
                                                  where req.OrderStatus == "report-generated"
                                                  && (req.BillingStatus == "paid" || req.BillingStatus == "unpaid")
                                                  //&& req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
                                                  && req.PatientId == PatientId
                                                  group new { req, patient, test } by new
                                                  {
                                                      patient,
                                                      req.SampleCode,
                                                      req.LabReportId,
                                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
                                                      req.VisitType,
                                                      req.RunNumberType,
                                                      report.IsPrinted,
                                                      req.BarCodeNumber,
                                                      req.WardName
                                                  } into grp
                                                  select new LabPendingResultVM
                                                  {
                                                      PatientId = grp.Key.patient.PatientId,
                                                      PatientCode = grp.Key.patient.PatientCode,
                                                      DateOfBirth = grp.Key.patient.DateOfBirth,
                                                      PhoneNumber = grp.Key.patient.PhoneNumber,
                                                      Gender = grp.Key.patient.Gender,
                                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

                                                      SampleCode = grp.Key.SampleCode,
                                                      SampleDate = grp.Key.Value,
                                                      SampleCodeFormatted = "",
                                                      VisitType = grp.Key.VisitType,
                                                      RunNumType = grp.Key.RunNumberType,
                                                      IsPrinted = grp.Key.IsPrinted,
                                                      BillingStatus = "paid",
                                                      BarCodeNumber = grp.Key.BarCodeNumber,
                                                      WardName = grp.Key.WardName,
                                                      ReportGeneratedBy = (from labRep in labDbContext.LabReports
                                                                           join employee in labDbContext.Employee
                                                                           on labRep.CreatedBy equals employee.EmployeeId
                                                                           where labRep.LabReportId == grp.Key.LabReportId
                                                                           select employee.FirstName + " " + (string.IsNullOrEmpty(employee.MiddleName) ? "" : employee.MiddleName + " ") + employee.LastName).FirstOrDefault(),

                                                      Tests = (from requisition in labDbContext.Requisitions
                                                               join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
                                                               where requisition.PatientId == PatientId
                                                               && requisition.SampleCode == grp.Key.SampleCode
                                                               && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
                                                               && requisition.OrderStatus == "report-generated"
                                                               && requisition.LabReportId == grp.Key.LabReportId
                                                               && requisition.VisitType == grp.Key.VisitType
                                                               && requisition.WardName == grp.Key.WardName
                                                               && requisition.RunNumberType == grp.Key.RunNumberType
                                                               && requisition.BarCodeNumber == grp.Key.BarCodeNumber
                                                               && (requisition.BillingStatus == "paid" || requisition.BillingStatus == "unpaid")

                                                               //group requisition by new { test } into g
                                                               select new LabPendingResultVM.LabTestDetail
                                                               {
                                                                   RequisitionId = requisition.RequisitionId,
                                                                   LabTestId = requisition.LabTestId,
                                                                   TestName = requisition.LabTestName
                                                                   //RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                                                                   //LabTestId = g.Key.test.LabTestId,
                                                                   //TestName = g.Key.test.LabTestName
                                                               }).ToList()
                                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

                    var finalReports = finalReportsProv.Union(finalReportsPaidUnpaid);
                    finalReports = finalReports.OrderByDescending(rep => rep.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();
                    List<LabPendingResultVM> finalReportList = new List<LabPendingResultVM>(finalReports);

                    LabMasterData.FinalReport = finalReportList;


                    var parameterOutPatWithProvisional = (from coreData in labDbContext.AdminParameters
                                                          where coreData.ParameterGroupName.ToLower() == "lab"
                                                          && coreData.ParameterName == "AllowLabReportToPrintOnProvisional"
                                                          select coreData.ParameterValue).FirstOrDefault();

                    bool allowOutPatWithProv = false;

                    if (!String.IsNullOrEmpty(parameterOutPatWithProvisional) && parameterOutPatWithProvisional.ToLower() == "true")
                    {
                        allowOutPatWithProv = true;
                    }


                    foreach (var rep in LabMasterData.FinalReport)
                    {
                        if (!String.IsNullOrEmpty(rep.VisitType) && !String.IsNullOrEmpty(rep.BillingStatus))
                        {
                            rep.IsValidToPrint = ValidatePrintOption(allowOutPatWithProv, rep.VisitType, rep.BillingStatus);
                        }
                        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
                    }

                    responseData.Results = LabMasterData;

                }


                else if (reqType == "labReportFromReqIdList")
                {
                    List<Int64> reqIdList = DanpheJSONConvert.DeserializeObject<List<Int64>>(requisitionIdList);
                    var allBarCode = (from requisition in labDbContext.Requisitions
                                      where reqIdList.Contains(requisition.RequisitionId)
                                      select requisition.BarCodeNumber).Distinct().ToList();
                    if (allBarCode != null && allBarCode.Count == 1)
                    {
                        LabReportVM labReport = DanpheEMR.Labs.LabsBL.GetLabReportVMForReqIds(labDbContext, reqIdList);
                        labReport.Lookups.SampleCodeFormatted = GetSampleCodeFormatted(labReport.Lookups.SampleCode, labReport.Lookups.SampleDate ?? default(DateTime), labReport.Lookups.VisitType, labReport.Lookups.RunNumberType);
                        labReport.ValidToPrint = true;
                        labReport.BarCodeNumber = allBarCode[0];
                        responseData.Results = labReport;
                    }
                    else
                    {
                        responseData.Status = "Failed";
                        responseData.ErrorMessage = "Multiple Barcode found for List of RequisitionID";
                    }

                }

                else if (reqType == "all-report-templates")
                {

                    List<LabReportTemplateModel> allReports = (from report in labDbContext.LabReportTemplates
                                                               where report.IsActive == true && report.TemplateType == "html"
                                                               select report).ToList();


                    responseData.Status = "OK";
                    responseData.Results = allReports;
                }

                //to view report of one patient-visit
                else if (reqType == "viewReport-visit")
                {
                    var viewReport = (from req in labDbContext.Requisitions
                                      join tst in labDbContext.LabTests on req.LabTestId equals tst.LabTestId
                                      join temp in labDbContext.LabReportTemplates on tst.ReportTemplateId equals temp.ReportTemplateID
                                      where req.PatientVisitId == patientVisitId && tst.ReportTemplateId == temp.ReportTemplateID
                                      select new
                                      {
                                          TemplateName = temp.ReportTemplateShortName,
                                          Components = (from res in labDbContext.LabTestComponentResults
                                                        where req.RequisitionId == req.RequisitionId
                                                        select new
                                                        {

                                                            Component = res.ComponentName,
                                                            Value = res.Value,
                                                            Unit = res.Unit,
                                                            Range = res.Range,
                                                            Remarks = res.Remarks,
                                                            CreatedOn = res.CreatedOn,
                                                            RequisitionId = res.RequisitionId,
                                                            IsAbnormal = res.IsAbnormal
                                                        }).ToList()
                                      }).FirstOrDefault();

                    responseData.Results = viewReport;

                }
                //to get the requisitions of only the given visit. update it later if needed for somewhere else.--sud-9Aug'17

                else if (reqType == "visit-requisitions")
                {
                    var reqsList = (from req in labDbContext.Requisitions
                                    where req.PatientVisitId == patientVisitId
                                    && req.PatientId == patientId
                                    select new
                                    {
                                        TestId = req.LabTestId,
                                        TestName = req.LabTestName
                                    }).ToList();

                    responseData.Results = reqsList;
                }

                //to view report of a patient
                else if (reqType == "viewReport-patient")
                {
                    var viewReport = (from req in labDbContext.Requisitions
                                      join tst in labDbContext.LabTests on req.LabTestId equals tst.LabTestId
                                      join temp in labDbContext.LabReportTemplates on tst.ReportTemplateId
                                      equals temp.ReportTemplateID
                                      where req.PatientId == patientId && tst.ReportTemplateId == temp.ReportTemplateID
                                      select new
                                      {
                                          TemplateName = temp.ReportTemplateShortName,
                                          Components = (from res in labDbContext.LabTestComponentResults
                                                        where res.RequisitionId == req.RequisitionId
                                                        select new
                                                        {
                                                            Date = req.OrderDateTime,
                                                            Component = res.ComponentName,
                                                            Value = res.Value,
                                                            Unit = res.Unit,
                                                            Range = res.Range,
                                                            Remarks = res.Remarks,
                                                            CreatedOn = res.CreatedOn,
                                                            RequisitionId = res.RequisitionId,
                                                            IsAbnormal = res.IsAbnormal

                                                        }).ToList()
                                      }).FirstOrDefault();

                    responseData.Results = viewReport;

                }


                //getting some data to show the report ..when print is order..
                else if (patientId != 0 && templateId != 0)
                {

                    var printReport = (from x in labDbContext.Patients
                                       join y in labDbContext.Requisitions on x.PatientId equals y.PatientId
                                       join z in labDbContext.LabTestComponentResults on y.RequisitionId equals z.RequisitionId
                                       where y.PatientId == patientId
                                       select new
                                       {
                                           PatientName = y.PatientName,
                                           DateOfBrith = x.DateOfBirth,
                                           Gender = x.Gender,
                                           PatientCode = x.PatientCode,
                                           CreatedOn = y.OrderDateTime,
                                           ProviderId = y.ProviderId,
                                           ProvierName = y.ProviderName,
                                           //LabTestCategory = y.LabTest.LabTestGroups.LabCategory.LabCategoryName,
                                           //sud: lab-refactoring:23May'18

                                       }).ToList();
                    responseData.Results = printReport;


                }

                //getting all the test for search box
                else if (inputValue != null)
                {
                    string returnValue = string.Empty;
                    List<LabTestModel> testNameListFrmCache = (List<LabTestModel>)DanpheCache.Get("lab-test-all");

                    List<LabTestModel> filteredList = new List<LabTestModel>();
                    if (string.IsNullOrEmpty(inputValue))
                    {
                        filteredList = testNameListFrmCache;
                    }
                    else
                    {
                        filteredList = (from t in testNameListFrmCache
                                            //add
                                        where t.LabTestName.ToLower().Contains(inputValue.ToLower())
                                        select t).ToList();
                    }

                    var formatedResult = new DanpheHTTPResponse<List<LabTestModel>>() { Results = filteredList };
                    returnValue = DanpheJSONConvert.SerializeObject(formatedResult, true);
                    return returnValue;

                }

                else if (reqType == "allLabTests")
                {

                    // to store in cache
                    List<LabTestModel> testsFromCache = (List<LabTestModel>)DanpheCache.Get("lab-test-all");
                    if (testsFromCache == null)
                    {
                        testsFromCache = (new DanpheEMR.DalLayer.LabDbContext(connString)).LabTests.ToList();
                        DanpheCache.Add("lab-test-all", testsFromCache, 5);
                    }
                    responseData.Results = testsFromCache;

                }

                else if (reqType == "labTestListOfSelectedInpatient")
                {
                    var currPatRequisitions = (from req in labDbContext.Requisitions
                                               join billItem in labDbContext.BillingTransactionItems on req.RequisitionId equals billItem.RequisitionId
                                               //into tempItmList
                                               join dept in labDbContext.ServiceDepartment on billItem.ServiceDepartmentId equals dept.ServiceDepartmentId

                                               where (req.PatientId == patientId) && (req.PatientVisitId == patientVisitId) && (billItem.PatientId == patientId)
                                                && (req.BillingStatus.ToLower() == "paid" || req.BillingStatus.ToLower() == "provisional")
                                                && (req.VisitType.ToLower() == "inpatient") && dept.IntegrationName == "LAB"
                                                && (!billItem.ReturnStatus.HasValue || billItem.ReturnStatus.Value == false)
                                               select new
                                               {
                                                   BillingTransactionItemId = billItem.BillingTransactionItemId,
                                                   RequisitionId = req.RequisitionId,
                                                   PatientId = req.PatientId,
                                                   PatientVisitId = req.PatientVisitId,
                                                   LabTestName = req.LabTestName,
                                                   LabTestId = req.LabTestId,
                                                   ReportTemplateId = req.ReportTemplateId,
                                                   LabTestSpecimen = req.LabTestSpecimen,
                                                   ProviderId = req.ProviderId,
                                                   ProviderName = req.ProviderName,
                                                   RunNumberType = req.RunNumberType,
                                                   BillingStatus = req.BillingStatus,
                                                   OrderStatus = req.OrderStatus,
                                                   OrderDateTime = req.OrderDateTime,
                                                   IsReportGenerated = (
                                                       (from cmp in labDbContext.LabTestComponentResults
                                                        where cmp.RequisitionId == req.RequisitionId
                                                       && cmp.LabReportId.HasValue
                                                        select cmp).ToList().Count > 0
                                                    )

                                               }).ToList();

                    responseData.Results = currPatRequisitions;

                }

                else if (reqType == "getSpecimen")
                {
                    LabRequisitionModel req = labDbContext.Requisitions.Where(val => val.RequisitionId == requisitionId).FirstOrDefault<LabRequisitionModel>();
                    if (req != null)
                    {
                        responseData.Results = req.LabTestSpecimen;
                    }
                }

                else if (reqType == "labRequisitionFromRequisitionIdList")
                {
                    List<Int64> reqIdList = DanpheJSONConvert.DeserializeObject<List<Int64>>(requisitionIdList);
                    List<LabRequisitionModel> allReq = new List<LabRequisitionModel>();
                    foreach (var reqId in reqIdList)
                    {
                        LabRequisitionModel eachReq = new LabRequisitionModel();
                        eachReq = labDbContext.Requisitions.Where(req => req.RequisitionId == reqId).FirstOrDefault();
                        allReq.Add(eachReq);
                    }

                    responseData.Results = allReq;
                }

                else if (reqType == "allTestListForExternalLabs")
                {
                    var defaultVendorId = (from vendor in labDbContext.LabVendors
                                           where vendor.IsDefault == true
                                           select vendor.LabVendorId).FirstOrDefault();
                    DateTime dtThirtyDays = DateTime.Now.AddDays(-30);
                    List<LabTestListWithVendor> allRequisitionsWithVendors = (from req in labDbContext.Requisitions
                                                                              join vendor in labDbContext.LabVendors on req.ResultingVendorId equals vendor.LabVendorId
                                                                              join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                                                              join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                                              where (req.OrderDateTime.HasValue ? req.OrderDateTime > dtThirtyDays : true)
                                                                              && req.OrderStatus == "pending" && req.ResultingVendorId == defaultVendorId
                                                                              select new LabTestListWithVendor
                                                                              {
                                                                                  PatientName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                                                  RequisitionId = req.RequisitionId,
                                                                                  VendorName = vendor.VendorName,
                                                                                  TestName = test.LabTestName
                                                                              }).ToList();
                    responseData.Results = allRequisitionsWithVendors;
                }

                else if (reqType == "allTestListSendToExternalLabs")
                {
                    var defaultVendorId = (from vendor in labDbContext.LabVendors
                                           where vendor.IsDefault == true
                                           select vendor.LabVendorId).FirstOrDefault();
                    DateTime dtThirtyDays = DateTime.Now.AddDays(-30);
                    List<LabTestListWithVendor> allRequisitionsWithVendors = (from req in labDbContext.Requisitions
                                                                              join vendor in labDbContext.LabVendors on req.ResultingVendorId equals vendor.LabVendorId
                                                                              join pat in labDbContext.Patients on req.PatientId equals pat.PatientId
                                                                              join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
                                                                              where (req.OrderDateTime.HasValue ? req.OrderDateTime > dtThirtyDays : true)
                                                                              && req.ResultingVendorId != defaultVendorId
                                                                              select new LabTestListWithVendor
                                                                              {
                                                                                  PatientName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                                                  RequisitionId = req.RequisitionId,
                                                                                  VendorName = vendor.VendorName,
                                                                                  TestName = test.LabTestName
                                                                              }).ToList();
                    responseData.Results = allRequisitionsWithVendors;
                }

                responseData.Status = "OK";

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return DanpheJSONConvert.SerializeObject(responseData, true);
        }


        // POST api/values
        [HttpPost]
        public string Post()
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                string reqType = this.ReadQueryStringData("reqType");
                string specimenDataModel = this.ReadQueryStringData("specimenData");
                string ipStr = this.ReadPostData();
                LabDbContext labDbContext = new LabDbContext(connString);
                RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                if (reqType != null && reqType == "AddComponent")
                {
                    using (var labDbContextTransaction = labDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            List<LabTestSpecimenModel> labSpecimenList = DanpheJSONConvert.DeserializeObject<List<LabTestSpecimenModel>>(specimenDataModel);
                            List<LabTestComponentResult> labComponentFromClient = DanpheJSONConvert.DeserializeObject<List<LabTestComponentResult>>(ipStr);

                            Int64 reqId = labComponentFromClient[0].RequisitionId;
                            int? templateId = labComponentFromClient[0].TemplateId;


                            LabRequisitionModel LabRequisition = labDbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault();

                            if (LabRequisition.ReportTemplateId != templateId)
                            {
                                UpdateReportTemplateId(reqId, templateId, labDbContext, currentUser.EmployeeId);
                                labComponentFromClient.ForEach(cmp =>
                                {
                                    cmp.CreatedOn = DateTime.Now;
                                    cmp.CreatedBy = currentUser.EmployeeId;
                                    labDbContext.LabTestComponentResults.Add(cmp);
                                });
                            }
                            else
                            {
                                labComponentFromClient.ForEach(cmp =>
                                {
                                    cmp.CreatedOn = DateTime.Now;
                                    cmp.CreatedBy = currentUser.EmployeeId;
                                    labDbContext.LabTestComponentResults.Add(cmp);
                                });

                            }

                            labDbContext.SaveChanges();




                            //once the results are saved, put the status of 
                            List<Int64> distinctRequisitions = labComponentFromClient.Select(a => a.RequisitionId).Distinct().ToList();

                            foreach (Int64 requisitionId in distinctRequisitions)
                            {
                                LabRequisitionModel dbRequisition = labDbContext.Requisitions
                                                                .Where(a => a.RequisitionId == requisitionId)
                                                                .FirstOrDefault<LabRequisitionModel>();

                                if (dbRequisition != null)
                                {
                                    dbRequisition.OrderStatus = "result-added";
                                    labDbContext.Entry(dbRequisition).Property(a => a.OrderStatus).IsModified = true;
                                }
                            }

                            labDbContext.SaveChanges();


                            //Add specimen of culture test
                            if (labSpecimenList != null && labSpecimenList.Count > 0)
                            {
                                int ln = labSpecimenList.Count;
                                for (int i = 0; i < ln; i++)
                                {
                                    int? requisitId = labSpecimenList[i].RequisitionId;
                                    string specimen = labSpecimenList[i].Specimen;
                                    if (requisitId != null && requisitId > 0)
                                    {
                                        LabRequisitionModel labReq = labDbContext.Requisitions.Where(val => val.RequisitionId == requisitId).FirstOrDefault<LabRequisitionModel>();
                                        labReq.LabTestSpecimen = specimen;
                                        labDbContext.SaveChanges();
                                    }
                                }
                            }


                            //PutOrderStatusOfRequisitions(labDbContext, distinctRequisitions, "final");
                            labDbContextTransaction.Commit();
                            responseData.Results = labComponentFromClient;
                        }
                        catch (Exception ex)
                        {
                            labDbContextTransaction.Rollback();
                            throw (ex);
                        }
                    }




                }
                else if (reqType == "FromBillingToRequisition")
                {
                    List<LabRequisitionModel> labReqListFromClient = DanpheJSONConvert.DeserializeObject<List<LabRequisitionModel>>(ipStr);
                    if (labReqListFromClient != null && labReqListFromClient.Count > 0)
                    {


                        PatientDbContext patientContext = new PatientDbContext(connString);
                        List<LabTestModel> allLabTests = labDbContext.LabTests.ToList();
                        int patId = labReqListFromClient[0].PatientId;
                        //get patient as querystring from client side rather than searching it from request's list.
                        PatientModel currPatient = patientContext.Patients.Where(p => p.PatientId == patId)
                            .FirstOrDefault<PatientModel>();

                        if (currPatient != null)
                        {

                            labReqListFromClient.ForEach(req =>
                            {
                                LabTestModel labTestdb = allLabTests.Where(a => a.LabTestId == req.LabTestId).FirstOrDefault<LabTestModel>();
                                //get PatientId from clientSide
                                if (labTestdb.IsValidSampling == true)
                                {
                                    req.LabTestSpecimen = labTestdb.LabTestSpecimen;
                                    req.LabTestSpecimenSource = labTestdb.LabTestSpecimenSource;
                                    req.OrderStatus = "active";
                                    req.LOINC = "LOINC Code";
                                    req.RunNumberType = labTestdb.RunNumberType;
                                    //req.PatientVisitId = visitId;//assign above visitid to this requisition.
                                    if (String.IsNullOrEmpty(currPatient.MiddleName))
                                        req.PatientName = currPatient.FirstName + " " + currPatient.LastName;
                                    else
                                        req.PatientName = currPatient.FirstName + " " + currPatient.MiddleName + " " + currPatient.LastName;

                                    req.OrderDateTime = DateTime.Now;
                                    labDbContext.Requisitions.Add(req);
                                }
                            });
                            labDbContext.SaveChanges();
                            responseData.Results = labReqListFromClient;
                        }
                    }
                    else
                    {
                        responseData.Status = "Failed";
                        responseData.ErrorMessage = "Invalid input request.";
                    }
                }
                else if (reqType == "addNewRequisitions") //comes here from doctor and nurse orders.
                {
                    List<LabRequisitionModel> labReqListFromClient = DanpheJSONConvert.DeserializeObject<List<LabRequisitionModel>>(ipStr);
                    LabVendorsModel defaultVendor = labDbContext.LabVendors.Where(val => val.IsDefault == true).FirstOrDefault();


                    if (labReqListFromClient != null && labReqListFromClient.Count > 0)
                    {
                        PatientDbContext patientContext = new PatientDbContext(connString);
                        List<LabTestModel> allLabTests = labDbContext.LabTests.ToList();
                        int patId = labReqListFromClient[0].PatientId;
                        //get patient as querystring from client side rather than searching it from request's list.
                        PatientModel currPatient = patientContext.Patients.Where(p => p.PatientId == patId)
                            .FirstOrDefault<PatientModel>();

                        if (currPatient != null)
                        {

                            labReqListFromClient.ForEach(req =>
                            {
                                req.ResultingVendorId = defaultVendor.LabVendorId;
                                LabTestModel labTestdb = allLabTests.Where(a => a.LabTestId == req.LabTestId).FirstOrDefault<LabTestModel>();
                                //get PatientId from clientSide
                                if (labTestdb.IsValidSampling == true)
                                {
                                    req.ReportTemplateId = labTestdb.ReportTemplateId ?? default(int);
                                    req.LabTestSpecimen = null;
                                    req.LabTestSpecimenSource = null;
                                    req.LabTestName = labTestdb.LabTestName;
                                    req.RunNumberType = labTestdb.RunNumberType;
                                    //req.OrderStatus = "active";
                                    req.LOINC = "LOINC Code";
                                    if (req.ProviderId != null && req.ProviderId != 0)
                                    {
                                        var emp = labDbContext.Employee.Where(a => a.EmployeeId == req.ProviderId).FirstOrDefault();
                                        req.ProviderName = emp.FullName;
                                    }

                                    //req.PatientVisitId = visitId;//assign above visitid to this requisition.
                                    if (String.IsNullOrEmpty(currPatient.MiddleName))
                                        req.PatientName = currPatient.FirstName + " " + currPatient.LastName;
                                    else
                                        req.PatientName = currPatient.FirstName + " " + currPatient.MiddleName + " " + currPatient.LastName;

                                    req.OrderDateTime = DateTime.Now;
                                    labDbContext.Requisitions.Add(req);
                                    labDbContext.SaveChanges();
                                }
                            });

                            responseData.Results = labReqListFromClient;
                            responseData.Status = "OK";
                        }
                    }
                    else
                    {
                        responseData.Status = "Failed";
                        responseData.ErrorMessage = "Invalid input request.";
                    }

                }

                else if (reqType == "add-labReport")
                {
                    using (var labDbTransaction = labDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            LabReportModel labReport = DanpheJSONConvert.DeserializeObject<LabReportModel>(ipStr);

                            var parameterData = (from parameter in labDbContext.AdminParameters
                                                 where parameter.ParameterGroupName.ToLower() == "lab"
                                                 && parameter.ParameterName == "AllowLabReportToPrintOnProvisional"
                                                 select parameter.ParameterValue).FirstOrDefault();


                            var VerificationEnabled = labReport.VerificationEnabled;
                            labReport.ReportingDate = DateTime.Now;
                            labReport.CreatedBy = currentUser.EmployeeId;
                            labReport.CreatedOn = labReport.ReportingDate;

                            List<Int64> reqIdList = new List<Int64>();
                            bool IsValidToPrint = true;


                            labDbContext.LabReports.Add(labReport);

                            labDbContext.SaveChanges();

                            if (labReport.LabReportId != 0)
                            {
                                foreach (var componentId in labReport.ComponentIdList)
                                {
                                    LabTestComponentResult component = labDbContext.LabTestComponentResults.Where(cmp => cmp.TestComponentResultId == componentId).FirstOrDefault();
                                    reqIdList.Add(component.RequisitionId);
                                    component.LabReportId = labReport.LabReportId;
                                    labDbContext.Entry(component).Property(a => a.LabReportId).IsModified = true;
                                }
                                labDbContext.SaveChanges();

                                var reqIdToUpdate = reqIdList.Distinct().ToList();

                                foreach (var reqId in reqIdToUpdate)
                                {
                                    LabRequisitionModel requisitionItem = labDbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault();
                                    if (VerificationEnabled == true)
                                    {
                                        //requisitionItem.OrderStatus = "report-generated";
                                    }
                                    else
                                    {
                                        requisitionItem.OrderStatus = "report-generated";
                                    }
                                    requisitionItem.LabReportId = labReport.LabReportId;

                                    //give provisional billing for outpatiient to print
                                    if (parameterData != null && (parameterData.ToLower() == "true" || parameterData == "1"))
                                    {
                                        if (requisitionItem.BillingStatus.ToLower() == "provisional")
                                        {
                                            IsValidToPrint = true;
                                        }

                                    }
                                    else
                                    {
                                        if ((requisitionItem.VisitType.ToLower() == "outpatient" || requisitionItem.VisitType.ToLower() == "emergency") && requisitionItem.BillingStatus.ToLower() == "provisional")
                                        {
                                            IsValidToPrint = false;
                                        }
                                    }

                                    if (requisitionItem.RunNumberType.ToLower() == "histo" || requisitionItem.RunNumberType.ToLower() == "cyto")
                                    {
                                        LabReportModel report = labDbContext.LabReports.Where(rep => rep.LabReportId == labReport.LabReportId).FirstOrDefault();
                                        report.ReceivingDate = requisitionItem.OrderDateTime;
                                        labReport.ReceivingDate = report.ReceivingDate;
                                    }
                                }
                                labDbContext.SaveChanges();

                                //if (docPatPortalSync)
                                //{
                                //    DocPatPortalBL.PostLabFinalReport(labReport, labDbContext);
                                //}
                            }


                            labDbTransaction.Commit();

                            labReport.ValidToPrint = IsValidToPrint;

                            responseData.Results = labReport;
                        }
                        catch (Exception ex)
                        {
                            labDbTransaction.Rollback();
                            throw (ex);
                        }
                    }


                }

                else if (reqType == "saveLabSticker")
                {
                    //ipDataString is input (HTML string)
                    if (ipStr.Length > 0)
                    {
                        ///api/Billing?reqType=saveLabSticker&PrinterName=sticker1809003399&FilePath=C:\DanpheHealthInc_PvtLtd_Files\Print\
                        //Read html

                        string PrinterName = this.ReadQueryStringData("PrinterName");
                        string FilePath = this.ReadQueryStringData("FilePath");
                        int noOfPrints = Convert.ToInt32(this.ReadQueryStringData("numOfCopies"));
                        if (noOfPrints == 0)
                        {
                            noOfPrints = 1;
                        }

                        for (int i = 0; i < noOfPrints; i++)
                        {
                            //index:i, taken in filename 
                            var fileName = "Lab_" + PrinterName + "_user_" + currentUser.EmployeeId + "_" + (i + 1) + ".html";
                            byte[] htmlbytearray = System.Text.Encoding.ASCII.GetBytes(ipStr);
                            //saving file to default folder, html file need to be delete after print is called.
                            System.IO.File.WriteAllBytes(@FilePath + fileName, htmlbytearray);

                        }

                        responseData.Status = "OK";
                        responseData.Results = 1;
                    }
                }
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return DanpheJSONConvert.SerializeObject(responseData, true);


        }


        // PUT api/values/5
        [HttpPut]
        public string Put()
        {

            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                //update Sample in LAB_Requisition
                string str = this.ReadPostData();
                string reqType = this.ReadQueryStringData("reqType");
                string billstatus = this.ReadQueryStringData("billstatus");
                string comments = this.ReadQueryStringData("comments");
                int vendorId = ToInt(this.ReadQueryStringData("vendorId"));
                //sud:22Aug'18 --it was giving error when trying Int.Parse(),  so use Convert.ToInt instead.
                int referredById = Convert.ToInt32(this.ReadQueryStringData("id"));

                int? SampleCode = ToInt(this.ReadQueryStringData("SampleCode"));
                int? PrintedReportId = ToInt(this.ReadQueryStringData("reportId"));
                int printid = 0;
                int.TryParse(this.ReadQueryStringData("printid"), out printid);
                RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                //int CurrentUser = 1;
                //int.TryParse(this.ReadQueryStringData("CurrentUser"), out CurrentUser);
                int? RunNumber = ToInt(this.ReadQueryStringData("RunNumber"));


                LabDbContext labDbContext = new LabDbContext(connString);

                LabRequisitionModel GetCurrentRequisitionData(string RunNumberType, string visitType, DateTime? sampleCreatedOn, int runNumber)
                {
                    LabRequisitionModel currRequisitionType = new LabRequisitionModel();
                    if (visitType.ToLower() == "inpatient" || visitType.ToLower() == "emergency")
                    {
                        if (RunNumberType.ToLower() == "normal")
                        {
                            currRequisitionType = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue) //this already filters not null data.. 
                                                   where sam.SampleCode == runNumber && sampleCreatedOn.Value.Year == sam.SampleCreatedOn.Value.Year
                                                   && (sam.VisitType.ToLower() == "inpatient" || sam.VisitType.ToLower() == "emergency") && sam.RunNumberType.ToLower() == RunNumberType
                                                   select sam).FirstOrDefault();
                        }
                        else
                        {
                            currRequisitionType = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue)
                                                   where sam.SampleCode == runNumber && sampleCreatedOn.Value.Year == sam.SampleCreatedOn.Value.Year
                                                   && sam.RunNumberType.ToLower() == RunNumberType
                                                   select sam).FirstOrDefault();
                        }

                    }
                    else if (visitType.ToLower() == "outpatient")
                    {
                        if (RunNumberType.ToLower() == "normal")
                        {
                            currRequisitionType = (from sam in labDbContext.Requisitions
                                                   where sam.SampleCode == runNumber
                                                   && DbFunctions.TruncateTime(sam.SampleCreatedOn).Value == DbFunctions.TruncateTime(sampleCreatedOn).Value
                                                   && sam.VisitType.ToLower() == "outpatient" && sam.RunNumberType.ToLower() == RunNumberType
                                                   select sam).FirstOrDefault();
                        }
                        else
                        {
                            currRequisitionType = (from sam in labDbContext.Requisitions.Where(r => r.SampleCreatedOn.HasValue)
                                                   where sam.SampleCode == runNumber && sampleCreatedOn.Value.Year == sam.SampleCreatedOn.Value.Year
                                                   && sam.RunNumberType.ToLower() == RunNumberType
                                                   select sam).FirstOrDefault();
                        }
                    }
                    else
                    {
                        currRequisitionType = null;
                    }

                    return currRequisitionType;
                }




                //used in collect sample page.
                //we're sending test list instead of reqId list because we may have different sample codes agaist different test if we use use last sample code feature.
                if (reqType == "updateSampleCode")
                {

                    List<PatientLabSampleVM> labTests = DanpheJSONConvert.DeserializeObject<List<PatientLabSampleVM>>(str); ;//this will come from client side--after parsing.
                    string RunNumberType = null;
                    string visitType = null;
                    DateTime? sampleCreatedOn = null;

                    //sample code for All Tests in Current Requests will be same.
                    int? sampleNum = null;
                    int? existingBarCodeNum = null;
                    int? LabBarCodeNum = null;




                    if (labTests != null)
                    {


                        using (var labDbTransaction = labDbContext.Database.BeginTransaction())
                        {
                            try
                            {


                                var requisitionid = labTests[0].RequisitionId;
                                LabRequisitionModel currRequisitionType = labDbContext.Requisitions
                                                                              .Where(a => a.RequisitionId == requisitionid)
                                                                              .FirstOrDefault<LabRequisitionModel>();


                                var barCodeList = (from v in labDbContext.LabBarCode
                                                   select v).ToList();
                                var BarCodeNumber = (barCodeList.Count > 0) ? barCodeList.Max(val => val.BarCodeNumber) + 1 : 1000000;


                                RunNumberType = currRequisitionType.RunNumberType;
                                visitType = currRequisitionType.VisitType;
                                sampleCreatedOn = labTests[0].SampleCreatedOn;
                                sampleNum = labTests[0].SampleCode;

                                currRequisitionType = GetCurrentRequisitionData(RunNumberType, visitType, sampleCreatedOn, (int)sampleNum);


                                if (currRequisitionType != null)
                                {
                                    existingBarCodeNum = currRequisitionType.BarCodeNumber;
                                    LabBarCodeModel newBarCode = labDbContext.LabBarCode
                                                                        .Where(c => c.BarCodeNumber == existingBarCodeNum)
                                                                        .FirstOrDefault<LabBarCodeModel>();
                                    newBarCode.IsActive = true;
                                    labDbContext.Entry(newBarCode).Property(a => a.IsActive).IsModified = true;
                                    labDbContext.SaveChanges();
                                }
                                else
                                {
                                    if (existingBarCodeNum == null)
                                    {
                                        LabBarCodeModel barCode = new LabBarCodeModel();
                                        barCode.BarCodeNumber = BarCodeNumber;
                                        barCode.IsActive = true;
                                        barCode.CreatedBy = currentUser.EmployeeId;
                                        barCode.CreatedOn = System.DateTime.Now;
                                        labDbContext.LabBarCode.Add(barCode);
                                        labDbContext.SaveChanges();
                                    }

                                }





                                foreach (var test in labTests)
                                {
                                    LabRequisitionModel dbRequisition = labDbContext.Requisitions
                                                                    .Where(a => a.RequisitionId == test.RequisitionId)
                                                                    .FirstOrDefault<LabRequisitionModel>();

                                    RunNumberType = dbRequisition.RunNumberType;


                                    if (test.SampleCode != null)
                                    {
                                        dbRequisition.SampleCode = sampleNum = test.SampleCode;
                                        dbRequisition.SampleCreatedOn = test.SampleCreatedOn;
                                        dbRequisition.SampleCreatedBy = currentUser.EmployeeId;
                                        dbRequisition.BarCodeNumber = existingBarCodeNum != null ? existingBarCodeNum : BarCodeNumber;
                                        visitType = dbRequisition.VisitType;
                                        sampleCreatedOn = test.SampleCreatedOn;
                                    }
                                    dbRequisition.LabTestSpecimen = test.Specimen;
                                    dbRequisition.OrderStatus = "pending";

                                    labDbContext.Entry(dbRequisition).Property(a => a.SampleCode).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.OrderStatus).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.RunNumberType).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.SampleCreatedBy).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.SampleCreatedOn).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.LabTestSpecimen).IsModified = true;
                                    labDbContext.Entry(dbRequisition).Property(a => a.BarCodeNumber).IsModified = true;
                                }

                                LabBarCodeNum = existingBarCodeNum != null ? existingBarCodeNum : BarCodeNumber;

                                labDbContext.SaveChanges();

                                labDbTransaction.Commit();

                                string formattedSampleCode = GetSampleCodeFormatted(sampleNum, sampleCreatedOn ?? default(DateTime), visitType, RunNumberType);
                                //string formattedSampleCode = DateTime.Now.ToString("yyMMdd") + "-" + sampleNum;
                                responseData.Results = new { FormattedSampleCode = formattedSampleCode, BarCodeNumber = LabBarCodeNum };
                                responseData.Status = "OK";
                            }
                            catch (Exception ex)
                            {
                                labDbTransaction.Rollback();
                                throw (ex);
                            }
                        }


                    }
                }
                //ashim: 20Sep2018
                //used in view report page page.
                else if (reqType == "updae-sample-code-reqId")
                {
                    List<Int64> reqIdList = DanpheJSONConvert.DeserializeObject<List<Int64>>(str);

                    DateTime? SampleDate = Convert.ToDateTime(this.ReadQueryStringData("SampleDate"));
                    string runNumberType = this.ReadQueryStringData("runNumberType");
                    string patVisitType = this.ReadQueryStringData("visitType");


                    int? existingBarCodeNum = null;


                    LabRequisitionModel requisition = new LabRequisitionModel();

                    var barCodeList = (from v in labDbContext.LabBarCode
                                       select v).ToList();
                    var BarCodeNumber = (barCodeList.Count > 0) ? barCodeList.Max(val => val.BarCodeNumber) + 1 : 1000000;


                    string visitType = null;
                    string RunNumberType = null;
                    int? LabBarCodeNum = null;


                    requisition = GetCurrentRequisitionData(runNumberType, patVisitType, SampleDate, (int)RunNumber);


                    if (requisition != null)
                    {
                        existingBarCodeNum = requisition.BarCodeNumber;
                        LabBarCodeModel newBarCode = labDbContext.LabBarCode
                                                            .Where(c => c.BarCodeNumber == existingBarCodeNum)
                                                            .FirstOrDefault<LabBarCodeModel>();
                        newBarCode.IsActive = true;

                        labDbContext.Entry(newBarCode).Property(a => a.IsActive).IsModified = true;

                        labDbContext.SaveChanges();
                    }
                    else
                    {
                        if (existingBarCodeNum == null)
                        {
                            LabBarCodeModel barCode = new LabBarCodeModel();
                            barCode.BarCodeNumber = BarCodeNumber;
                            barCode.IsActive = true;
                            barCode.CreatedBy = currentUser.EmployeeId;
                            barCode.CreatedOn = System.DateTime.Now;
                            labDbContext.LabBarCode.Add(barCode);
                            labDbContext.SaveChanges();
                        }

                    }


                    foreach (var reqId in reqIdList)
                    {
                        LabRequisitionModel dbRequisition = labDbContext.Requisitions
                                                        .Where(a => a.RequisitionId == reqId)
                                                        .FirstOrDefault<LabRequisitionModel>();


                        if (dbRequisition != null)
                        {
                            List<LabRequisitionModel> allReqWithCurrBarcode = labDbContext.Requisitions
                                                                                .Where(r => r.BarCodeNumber == dbRequisition.BarCodeNumber)
                                                                                .ToList();


                            if (allReqWithCurrBarcode.Count == reqIdList.Count)
                            {
                                LabBarCodeModel oldBarCode = labDbContext.LabBarCode
                                                            .Where(c => c.BarCodeNumber == dbRequisition.BarCodeNumber)
                                                            .FirstOrDefault<LabBarCodeModel>();
                                oldBarCode.IsActive = false;
                                oldBarCode.ModifiedBy = currentUser.EmployeeId;
                                oldBarCode.ModifiedOn = System.DateTime.Now;
                                labDbContext.Entry(oldBarCode).Property(a => a.ModifiedBy).IsModified = true;
                                labDbContext.Entry(oldBarCode).Property(a => a.ModifiedOn).IsModified = true;
                                labDbContext.Entry(oldBarCode).Property(a => a.IsActive).IsModified = true;
                            }





                            dbRequisition.SampleCode = RunNumber;
                            // dbRequisition.SampleCode = sampleNum;
                            dbRequisition.SampleCreatedOn = SampleDate;
                            dbRequisition.SampleCreatedBy = currentUser.EmployeeId;
                            dbRequisition.ModifiedBy = currentUser.EmployeeId;
                            dbRequisition.ModifiedOn = System.DateTime.Now;
                            dbRequisition.BarCodeNumber = existingBarCodeNum != null ? existingBarCodeNum : BarCodeNumber;
                            visitType = dbRequisition.VisitType;
                            RunNumberType = dbRequisition.RunNumberType;
                            LabBarCodeNum = existingBarCodeNum != null ? existingBarCodeNum : BarCodeNumber;
                        }

                        labDbContext.Entry(dbRequisition).Property(a => a.ModifiedBy).IsModified = true;
                        labDbContext.Entry(dbRequisition).Property(a => a.ModifiedOn).IsModified = true;
                        labDbContext.Entry(dbRequisition).Property(a => a.SampleCode).IsModified = true;
                        labDbContext.Entry(dbRequisition).Property(a => a.SampleCreatedBy).IsModified = true;
                        labDbContext.Entry(dbRequisition).Property(a => a.SampleCreatedOn).IsModified = true;
                        labDbContext.Entry(dbRequisition).Property(a => a.BarCodeNumber).IsModified = true;
                    }


                    labDbContext.SaveChanges();
                    string formattedSampleCode = GetSampleCodeFormatted(RunNumber, SampleDate ?? default(DateTime), visitType, RunNumberType);
                    responseData.Results = new { FormattedSampleCode = formattedSampleCode, BarCodeNumber = LabBarCodeNum };
                    responseData.Status = "OK";
                }

                else if (reqType == "updateBillStatus" && billstatus != null)
                {
                    BillingDbContext billDbContext = new BillingDbContext(connString);
                    List<int> reqIds = DanpheJSONConvert.DeserializeObject<List<int>>(str);
                    foreach (var reqId in reqIds)
                    {
                        LabRequisitionModel dbrequisition = labDbContext.Requisitions
                                                        .Where(a => a.RequisitionId == reqId)
                                                        .FirstOrDefault<LabRequisitionModel>();

                        //VisitType could be changed in case of copy from earlier invoice.
                        BillingTransactionItemModel billTxnItem = (from item in billDbContext.BillingTransactionItems
                                                                   join srvDept in billDbContext.ServiceDepartment on item.ServiceDepartmentId equals srvDept.ServiceDepartmentId
                                                                   where srvDept.IntegrationName.ToLower() == "lab" && item.RequisitionId == reqId
                                                                   select item).OrderByDescending(a => a.BillingTransactionItemId).FirstOrDefault();
                        if (billTxnItem != null)
                        {
                            dbrequisition.VisitType = billTxnItem.VisitType;
                        }

                        dbrequisition.BillingStatus = billstatus;
                        dbrequisition.ModifiedBy = currentUser.EmployeeId;
                        dbrequisition.ModifiedOn = DateTime.Now;
                        labDbContext.Entry(dbrequisition).Property(a => a.BillingStatus).IsModified = true;
                        labDbContext.Entry(dbrequisition).Property(a => a.VisitType).IsModified = true;
                        labDbContext.Entry(dbrequisition).Property(a => a.ModifiedBy).IsModified = true;
                        labDbContext.Entry(dbrequisition).Property(a => a.ModifiedOn).IsModified = true;
                        //labDbContext.Entry(dbrequisition).State = EntityState.Modified;
                    }
                    labDbContext.SaveChanges();
                    responseData.Results = "lab Billing Status  updated successfully.";
                }


                else if (reqType == "UpdateCommentsOnTestRequisition")
                {
                    List<Int64> RequisitionIds = (DanpheJSONConvert.DeserializeObject<List<Int64>>(str));
                    //int newPrintId = printid + 1;
                    foreach (Int64 reqId in RequisitionIds)
                    {
                        List<LabRequisitionModel> listTestReq = labDbContext.Requisitions
                                                 .Where(a => a.RequisitionId == reqId)
                                                 .ToList<LabRequisitionModel>();
                        if (listTestReq != null)
                        {
                            foreach (var reqResult in listTestReq)
                            {
                                reqResult.Comments = comments;
                                //labDbContext.Entry(reqResult).State = EntityState.Modified;
                                labDbContext.Entry(reqResult).Property(a => a.Comments).IsModified = true;

                            }


                        }
                    }

                    labDbContext.SaveChanges();
                    responseData.Status = "OK";
                    responseData.Results = RequisitionIds;
                }





                // to update the lab result
                else if (reqType == "EditLabTestResult")
                {
                    string specimenDataModel = this.ReadQueryStringData("specimenData");
                    List<LabTestComponentResult> labtestsresults = DanpheJSONConvert.
                        DeserializeObject<List<LabTestComponentResult>>(str);
                    List<LabTestSpecimenModel> labSpecimenList = DanpheJSONConvert.DeserializeObject<List<LabTestSpecimenModel>>(specimenDataModel);
                    if (labtestsresults != null && labtestsresults.Count > 0)
                    {

                        var useNewMethod = true;//sud: use earlier method if this doesn't work correctly


                        if (useNewMethod)
                        {
                            EditComponentsResults(labDbContext, labtestsresults, currentUser);

                            //Update specimen of culture test
                            if (labSpecimenList != null && labSpecimenList.Count > 0)
                            {
                                int ln = labSpecimenList.Count;
                                for (int i = 0; i < ln; i++)
                                {
                                    int? requisitId = labSpecimenList[i].RequisitionId;
                                    string specimen = labSpecimenList[i].Specimen;
                                    if (requisitId != null && requisitId > 0)
                                    {
                                        LabRequisitionModel labReq = labDbContext.Requisitions.Where(val => val.RequisitionId == requisitId).FirstOrDefault<LabRequisitionModel>();
                                        labReq.LabTestSpecimen = specimen;
                                        labDbContext.SaveChanges();
                                    }
                                }
                            }

                            responseData.Status = "OK";
                            responseData.Results = new List<LabTestComponentResult>();
                        }
                        else
                        {
                            List<LabTestComponentResult> compsToUpdate = labtestsresults.Where(comp => comp.TestComponentResultId != 0).ToList();
                            List<LabTestComponentResult> compsToInsert = labtestsresults.Where(comp => comp.TestComponentResultId == 0).ToList();

                            var reportId = compsToInsert[0].LabReportId;

                            foreach (var labtestres in compsToUpdate)
                            {
                                LabTestComponentResult TestComp = labDbContext.LabTestComponentResults
                                                     .Where(a => a.TestComponentResultId == labtestres.TestComponentResultId)
                                                      .FirstOrDefault<LabTestComponentResult>();


                                TestComp.LabReportId = reportId;
                                TestComp.Value = labtestres.Value;
                                TestComp.Remarks = labtestres.Remarks;
                                TestComp.IsAbnormal = labtestres.IsAbnormal;
                                TestComp.AbnormalType = labtestres.AbnormalType;
                                TestComp.ModifiedOn = DateTime.Now;
                                TestComp.ModifiedBy = currentUser.EmployeeId;
                                labDbContext.Entry(TestComp).Property(a => a.LabReportId).IsModified = true;
                                labDbContext.Entry(TestComp).Property(a => a.Value).IsModified = true;
                                labDbContext.Entry(TestComp).Property(a => a.IsAbnormal).IsModified = true;
                                labDbContext.Entry(TestComp).Property(a => a.AbnormalType).IsModified = true;
                                labDbContext.Entry(TestComp).Property(a => a.ModifiedOn).IsModified = true;
                                labDbContext.Entry(TestComp).Property(a => a.ModifiedBy).IsModified = true;


                                //labDbContext.Entry(TestComp).State = EntityState.Modified;
                            }
                            labDbContext.SaveChanges();

                            //Add Extra added Components from FrontEnd Side
                            compsToInsert.ForEach(cmp =>
                            {
                                cmp.CreatedOn = DateTime.Now;
                                cmp.CreatedBy = currentUser.EmployeeId;
                                cmp.IsActive = true;
                                labDbContext.LabTestComponentResults.Add(cmp);

                                labtestsresults.Add(cmp);
                            });

                            labDbContext.SaveChanges();

                            responseData.Status = "OK";
                            responseData.Results = new List<LabTestComponentResult>();
                        }
                    }
                    else
                    {
                        responseData.Status = "Failed";
                        responseData.ErrorMessage = "Empty Component Sets";
                    }


                }
                else if (reqType == "update-labReport")
                {
                    LabReportModel clientReport = DanpheJSONConvert.DeserializeObject<LabReportModel>(str);
                    LabReportModel servReport = labDbContext.LabReports
                                             .Where(a => a.LabReportId == clientReport.LabReportId)
                                              .FirstOrDefault<LabReportModel>();

                    if (servReport != null)
                    {
                        servReport.ModifiedBy = currentUser.EmployeeId;
                        servReport.ModifiedOn = DateTime.Now;
                        servReport.Signatories = clientReport.Signatories;
                        servReport.Comments = clientReport.Comments;
                    }
                    labDbContext.Entry(servReport).Property(a => a.Signatories).IsModified = true;
                    labDbContext.Entry(servReport).Property(a => a.ModifiedOn).IsModified = true;
                    labDbContext.Entry(servReport).Property(a => a.ModifiedBy).IsModified = true;
                    labDbContext.Entry(servReport).Property(a => a.Comments).IsModified = true;
                    labDbContext.SaveChanges();
                    responseData.Status = "OK";
                }

                else if (reqType == "update-reportPrintedFlag")
                {
                    int? repId = PrintedReportId;
                    if (repId != null && repId > 0)
                    {
                        LabReportModel report = labDbContext.LabReports.Where(val => val.LabReportId == repId).FirstOrDefault<LabReportModel>();
                        labDbContext.LabReports.Attach(report);

                        labDbContext.Entry(report).Property(a => a.IsPrinted).IsModified = true;
                        labDbContext.Entry(report).Property(a => a.PrintedOn).IsModified = true;
                        labDbContext.Entry(report).Property(a => a.PrintedBy).IsModified = true;
                        labDbContext.Entry(report).Property(a => a.PrintCount).IsModified = true;

                        report.IsPrinted = true;
                        report.PrintedOn = System.DateTime.Now;
                        report.PrintedBy = currentUser.EmployeeId;
                        report.PrintCount = report.PrintCount + 1;

                        labDbContext.SaveChanges();
                        responseData.Results = report;
                        responseData.Status = "OK";

                    }

                }

                else if (reqType == "UpdateDoctor")
                {
                    //update doctor name for here.. 
                    List<Int32> reqList = DanpheJSONConvert.DeserializeObject<List<Int32>>(str);

                    int reffByDocId = referredById;
                    string refferedByDoctorName = (from emp in labDbContext.Employee
                                                   where emp.EmployeeId == reffByDocId
                                                   select emp.LongSignature
                                                   ).FirstOrDefault<string>();

                    foreach (int req in reqList)
                    {
                        LabRequisitionModel labReq = labDbContext.Requisitions
                                             .Where(a => a.RequisitionId == req)
                                              .FirstOrDefault<LabRequisitionModel>();

                        labDbContext.Requisitions.Attach(labReq);

                        labDbContext.Entry(labReq).Property(a => a.ProviderId).IsModified = true;
                        labDbContext.Entry(labReq).Property(a => a.ProviderName).IsModified = true;
                        labDbContext.Entry(labReq).Property(a => a.ModifiedOn).IsModified = true;
                        labDbContext.Entry(labReq).Property(a => a.ModifiedBy).IsModified = true;

                        labReq.ProviderName = refferedByDoctorName;
                        labReq.ProviderId = reffByDocId;
                        labReq.ModifiedBy = currentUser.EmployeeId;
                        labReq.ModifiedOn = DateTime.Now;
                        labDbContext.SaveChanges();
                    }

                    responseData.Status = "OK";

                }
                else if (reqType == "UpdateDoctorNameInLabReport")
                {
                    int id = Convert.ToInt32(this.ReadQueryStringData("id"));

                    LabReportModel labreport = labDbContext.LabReports
                        .Where(rep => rep.LabReportId == id).FirstOrDefault<LabReportModel>();

                    labDbContext.LabReports.Attach(labreport);

                    labDbContext.Entry(labreport).Property(a => a.ReferredByDr).IsModified = true;
                    labDbContext.Entry(labreport).Property(a => a.ModifiedOn).IsModified = true;
                    labDbContext.Entry(labreport).Property(a => a.ModifiedBy).IsModified = true;

                    labreport.ReferredByDr = str;
                    labreport.ModifiedBy = currentUser.EmployeeId;
                    labreport.ModifiedOn = DateTime.Now;
                    labDbContext.SaveChanges();

                    responseData.Status = "OK";
                }
                else if (reqType == "ChangeLabTestWithSamePrice")
                {

                    using (var dbContextTransaction = labDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            int reqId = Convert.ToInt32(this.ReadQueryStringData("requisitionid"));
                            //var ChangedLabTest = JsonConvert.DeserializeAnonymousType(str, BillItemVM);
                            LabTestTransactionItemVM ChangedLabTest = DanpheJSONConvert.DeserializeObject<LabTestTransactionItemVM>(str);

                            var labServiceDeptList = (from dpt in labDbContext.Department
                                                      join serviceDept in labDbContext.ServiceDepartment on dpt.DepartmentId equals serviceDept.DepartmentId
                                                      where dpt.DepartmentName.ToLower() == "lab"
                                                      select serviceDept.ServiceDepartmentId).ToList();

                            BillingTransactionItemModel itemTransaction = (from billItem in labDbContext.BillingTransactionItems
                                                                           where billItem.RequisitionId == reqId && labServiceDeptList.Contains(billItem.ServiceDepartmentId)
                                                                           select billItem).FirstOrDefault<BillingTransactionItemModel>();

                            labDbContext.BillingTransactionItems.Attach(itemTransaction);
                            labDbContext.Entry(itemTransaction).Property(a => a.ItemId).IsModified = true;
                            labDbContext.Entry(itemTransaction).Property(a => a.ItemName).IsModified = true;
                            labDbContext.Entry(itemTransaction).Property(a => a.ServiceDepartmentId).IsModified = true;
                            labDbContext.Entry(itemTransaction).Property(a => a.ServiceDepartmentName).IsModified = true;

                            itemTransaction.ItemId = ChangedLabTest.ItemId;
                            itemTransaction.ItemName = ChangedLabTest.ItemName;
                            itemTransaction.ServiceDepartmentId = ChangedLabTest.ServiceDepartmentId;
                            itemTransaction.ServiceDepartmentName = ChangedLabTest.ServiceDepartmentName;

                            labDbContext.SaveChanges();

                            LabRequisitionModel labReq = labDbContext.Requisitions
                                                        .Where(val => val.RequisitionId == reqId)
                                                        .FirstOrDefault<LabRequisitionModel>();

                            LabTestModel labTest = labDbContext.LabTests.
                                                   Where(val => val.LabTestId == ChangedLabTest.ItemId)
                                                   .FirstOrDefault<LabTestModel>();

                            LabReportTemplateModel defRptTempModel = labDbContext.LabReportTemplates.
                                                        Where(val => val.IsDefault == true)
                                                        .FirstOrDefault();

                            labDbContext.Requisitions.Attach(labReq);
                            labDbContext.Entry(labReq).Property(a => a.LabTestId).IsModified = true;
                            labDbContext.Entry(labReq).Property(a => a.LabTestName).IsModified = true;
                            labDbContext.Entry(labReq).Property(a => a.ReportTemplateId).IsModified = true;
                            labDbContext.Entry(labReq).Property(a => a.RunNumberType).IsModified = true;


                            labReq.LabTestName = ChangedLabTest.ItemName;
                            labReq.LabTestId = ChangedLabTest.ItemId;
                            labReq.RunNumberType = labTest.RunNumberType;

                            int newRptTempId = 1;//hardcoded value

                            if (defRptTempModel != null)
                            {
                                newRptTempId = defRptTempModel.ReportTemplateID;
                            }
                            labReq.ReportTemplateId = labTest.ReportTemplateId.HasValue ? (int)labTest.ReportTemplateId : newRptTempId;

                            labDbContext.SaveChanges();
                            dbContextTransaction.Commit();

                            responseData.Status = "OK";
                            responseData.Results = labTest;


                        }
                        catch (Exception ex)
                        {
                            dbContextTransaction.Rollback();
                            throw (ex);
                        }
                    }
                }

                else if (reqType == "cancelInpatientLabTest")
                {

                    using (var labDbContextTransaction = labDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            InpatientLabTestModel inpatientLabTest = DanpheJSONConvert.DeserializeObject<InpatientLabTestModel>(str);



                            BillingTransactionItemModel billItem = labDbContext.BillingTransactionItems
                                                                    .Where(itm =>
                                                                            itm.RequisitionId == inpatientLabTest.RequisitionId
                                                                            && itm.ItemId == inpatientLabTest.LabTestId
                                                                            && itm.PatientId == inpatientLabTest.PatientId
                                                                            && itm.PatientVisitId == inpatientLabTest.PatientVisitId
                                                                            && itm.BillingType.ToLower() == "inpatient"
                                                                            && itm.BillStatus.ToLower() != "paid"
                                                                            && itm.BillingTransactionItemId == inpatientLabTest.BillingTransactionItemId
                                                                        ).FirstOrDefault<BillingTransactionItemModel>();

                            labDbContext.BillingTransactionItems.Attach(billItem);

                            labDbContext.Entry(billItem).Property(a => a.BillStatus).IsModified = true;
                            labDbContext.Entry(billItem).Property(a => a.CancelledBy).IsModified = true;
                            labDbContext.Entry(billItem).Property(a => a.CancelledOn).IsModified = true;
                            labDbContext.Entry(billItem).Property(a => a.CancelRemarks).IsModified = true;

                            billItem.BillStatus = "cancel";
                            billItem.CancelledBy = currentUser.EmployeeId;
                            billItem.CancelledOn = System.DateTime.Now;
                            billItem.CancelRemarks = inpatientLabTest.CancelRemarks;
                            labDbContext.SaveChanges();



                            LabRequisitionModel labReq = labDbContext.Requisitions
                                                            .Where(req => req.RequisitionId == inpatientLabTest.RequisitionId
                                                                && (req.VisitType.ToLower() == "inpatient" || req.VisitType.ToLower() == "emergency")
                                                                && req.BillingStatus.ToLower() != "paid"
                                                            ).FirstOrDefault<LabRequisitionModel>();


                            labDbContext.Requisitions.Attach(labReq);

                            labDbContext.Entry(labReq).Property(a => a.BillingStatus).IsModified = true;

                            labReq.BillingStatus = "cancel";
                            labDbContext.SaveChanges();

                            labDbContextTransaction.Commit();

                            responseData.Status = "OK";

                        }
                        catch (Exception ex)
                        {
                            labDbContextTransaction.Rollback();
                            throw (ex);
                        }
                    }
                }

                else if (reqType == "update-specimen")
                {
                    int reqId = Convert.ToInt32(this.ReadQueryStringData("ReqId"));
                    string specimen = this.ReadQueryStringData("Specimen");
                    if (reqId > 0)
                    {
                        LabRequisitionModel labReq = labDbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault<LabRequisitionModel>();
                        labReq.LabTestSpecimen = specimen;
                        labReq.ModifiedBy = currentUser.EmployeeId;
                        labReq.ModifiedOn = DateTime.Now;
                        labDbContext.Entry(labReq).Property(a => a.LabTestSpecimen).IsModified = true;
                        labDbContext.Entry(labReq).Property(a => a.ModifiedBy).IsModified = true;
                        labDbContext.Entry(labReq).Property(a => a.ModifiedOn).IsModified = true;
                        labDbContext.SaveChanges();
                    }
                    responseData.Status = "OK";
                    responseData.Results = specimen;
                }

                else if (reqType == "undo-samplecode")
                {
                    List<Int64> RequisitionIds = (DanpheJSONConvert.DeserializeObject<List<Int64>>(str));
                    //int newPrintId = printid + 1;
                    foreach (Int64 reqId in RequisitionIds)
                    {
                        List<LabRequisitionModel> listTestReq = labDbContext.Requisitions
                                                 .Where(a => a.RequisitionId == reqId)
                                                 .ToList<LabRequisitionModel>();
                        if (listTestReq != null)
                        {
                            foreach (var reqResult in listTestReq)
                            {
                                reqResult.SampleCode = null;
                                reqResult.SampleCreatedBy = null;
                                reqResult.SampleCreatedOn = null;
                                reqResult.OrderStatus = "active";
                                reqResult.ModifiedBy = currentUser.EmployeeId;
                                reqResult.ModifiedOn = DateTime.Now;
                                reqResult.LabTestSpecimen = null;
                                reqResult.LabTestSpecimenSource = null;
                                reqResult.BarCodeNumber = null;

                                labDbContext.Entry(reqResult).Property(a => a.SampleCode).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.SampleCreatedBy).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.SampleCreatedOn).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.OrderStatus).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.ModifiedBy).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.ModifiedOn).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.LabTestSpecimen).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.LabTestSpecimenSource).IsModified = true;
                                labDbContext.Entry(reqResult).Property(a => a.BarCodeNumber).IsModified = true;

                            }


                        }
                    }

                    labDbContext.SaveChanges();
                    responseData.Status = "OK";
                    responseData.Results = RequisitionIds;
                }

                else if (reqType == "verify-all-labtests")
                {
                    LabReportModel labReport = DanpheJSONConvert.DeserializeObject<LabReportModel>(str);
                    var VerificationEnabled = labReport.VerificationEnabled;


                    List<Int64> reqIdList = new List<Int64>();

                    if (VerificationEnabled != null && VerificationEnabled == true)
                    {
                        if (labReport.LabReportId != 0)
                        {
                            foreach (var componentId in labReport.ComponentIdList)
                            {
                                LabTestComponentResult component = labDbContext.LabTestComponentResults.Where(cmp => cmp.TestComponentResultId == componentId).FirstOrDefault();
                                reqIdList.Add(component.RequisitionId);
                            }


                            var reqIdToUpdate = reqIdList.Distinct().ToList();

                            foreach (var reqId in reqIdToUpdate)
                            {
                                LabRequisitionModel requisitionItem = labDbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault();

                                requisitionItem.OrderStatus = "report-generated";
                                requisitionItem.VerifiedBy = currentUser.EmployeeId;
                                requisitionItem.VerifiedOn = DateTime.Now;
                                requisitionItem.IsVerified = true;

                                labDbContext.Entry(requisitionItem).Property(a => a.OrderStatus).IsModified = true;
                                labDbContext.Entry(requisitionItem).Property(a => a.VerifiedBy).IsModified = true;
                                labDbContext.Entry(requisitionItem).Property(a => a.VerifiedOn).IsModified = true;
                                labDbContext.Entry(requisitionItem).Property(a => a.IsVerified).IsModified = true;

                                labDbContext.SaveChanges();
                            }

                        }

                    }

                    responseData.Status = "OK";
                    responseData.Results = labReport;
                }

                else if (reqType == "verify-all-requisitions-directly")
                {
                    List<Int64> reqIdList = DanpheJSONConvert.DeserializeObject<List<Int64>>(str);

                    foreach (var reqId in reqIdList)
                    {
                        LabRequisitionModel requisitionItem = labDbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault();

                        requisitionItem.OrderStatus = "report-generated";
                        requisitionItem.VerifiedBy = currentUser.EmployeeId;
                        requisitionItem.VerifiedOn = DateTime.Now;
                        requisitionItem.IsVerified = true;

                        labDbContext.Entry(requisitionItem).Property(a => a.OrderStatus).IsModified = true;
                        labDbContext.Entry(requisitionItem).Property(a => a.VerifiedBy).IsModified = true;
                        labDbContext.Entry(requisitionItem).Property(a => a.VerifiedOn).IsModified = true;
                        labDbContext.Entry(requisitionItem).Property(a => a.IsVerified).IsModified = true;

                        labDbContext.SaveChanges();
                    }

                    responseData.Status = "OK";
                }

                else if (reqType == "UpdateVendorIdToLabTestRequisition")
                {
                    var newVendorId = vendorId;
                    List<Int64> RequisitionIds = (DanpheJSONConvert.DeserializeObject<List<Int64>>(str));
                    foreach (Int64 reqId in RequisitionIds)
                    {
                        LabRequisitionModel singleRequisition = labDbContext.Requisitions
                                                  .Where(a => a.RequisitionId == reqId)
                                                  .FirstOrDefault();
                        if (singleRequisition != null)
                        {
                            singleRequisition.ResultingVendorId = newVendorId;
                            labDbContext.Entry(singleRequisition).Property(a => a.ResultingVendorId).IsModified = true;
                            labDbContext.SaveChanges();
                        }
                    }

                    responseData.Status = "OK";
                }

                else
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "Invalid request type.";
                }

                #region //Commented codes. Delete ASAP
                // updating Printstatus on TestComponent result table
                //else if (reqType == "UpdatePrintStatusForReport")
                //{
                //    List<Int64> RequisitionIds = (DanpheJSONConvert.DeserializeObject<List<Int64>>(str));
                //    foreach (Int64 reqId in RequisitionIds)
                //    {
                //        List<LabTestComponentResult> listTestComp = labDbContext.LabTestComponents
                //                                 .Where(a => a.RequisitionId == reqId)
                //                                 .ToList<LabTestComponentResult>();
                //        if (listTestComp != null)
                //        {
                //            foreach (var compResult in listTestComp)
                //            {
                //                compResult.IsPrint = true;
                //                compResult.LabReportId = printid;
                //                labDbContext.Entry(compResult).Property(a => a.IsPrint).IsModified = true;
                //                labDbContext.Entry(compResult).Property(a => a.LabReportId).IsModified = true;
                //                //labDbContext.Entry(compResult).State = EntityState.Modified;
                //            }
                //        }
                //    }
                //    labDbContext.SaveChanges();
                //    responseData.Status = "OK";
                //    responseData.Results = RequisitionIds;
                //}

                //updating doctors remark
                //else if (reqType != null && reqType == "AddDoctorsRemark")
                //{

                //    List<LabTestComponentResult> labtestresults = DanpheJSONConvert.DeserializeObject<List<LabTestComponentResult>>(str); ;

                //    foreach (var labtestresult in labtestresults)
                //    {

                //        LabTestComponentResult dbresult = labDbContext.LabResults
                //                                        .Where(a => a.RequisitionId == labtestresult.RequisitionId)
                //                                        .FirstOrDefault<LabTestComponentResult>();
                //        dbresult.DoctorsRemark = labtestresult.DoctorsRemark;

                //        labDbContext.Entry(dbresult).State = EntityState.Modified;
                //    }
                //    labDbContext.SaveChanges();
                //    responseData.Results = "lab Doctors Remark  updated successfully.";
                //}

                ////updating labOrderStatus
                //else
                //{

                //    List<LabTestComponentResult> labtestsresults = DanpheJSONConvert.
                //        DeserializeObject<List<LabTestComponentResult>>(str);

                //    foreach (var labtestresult in labtestsresults)
                //    {
                //        LabRequisitionModel dbRequisition = labDbContext.Requisitions
                //                                        .Where(a => a.RequisitionId == labtestresult.RequisitionId)
                //                                        .FirstOrDefault<LabRequisitionModel>();

                //        dbRequisition.OrderStatus = "final";
                //        labDbContext.Entry(dbRequisition).State = EntityState.Modified;
                //    }
                //    labDbContext.SaveChanges();
                //    responseData.Results = "lab Order Status  updated successfully.";
                //}
                #endregion
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return DanpheJSONConvert.SerializeObject(responseData, true);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private bool PutOrderStatusOfRequisitions(LabDbContext labDbContext, List<Int64> requisitionIds, string orderStatus)
        {
            foreach (Int64 reqId in requisitionIds)
            {
                LabRequisitionModel dbRequisition = labDbContext.Requisitions
                                                .Where(a => a.RequisitionId == reqId)
                                                .FirstOrDefault<LabRequisitionModel>();

                if (dbRequisition != null)
                {
                    dbRequisition.OrderStatus = orderStatus;
                    labDbContext.Entry(dbRequisition).Property(a => a.OrderStatus).IsModified = true;
                }
            }
            labDbContext.SaveChanges();

            return true;

        }


        //Generate unique Sample code for current request.
        //sample code Is Reset to 1 everyday.
        private int? GetOutpatientLatestSampleSequence(LabDbContext labDbContext, DateTime SampleDate)
        {
            //default samplecode is 1
            int newSampleSequence = 1;

            // 'where' condition to get samplecode of todays date only
            var latestSample = (from req in labDbContext.Requisitions
                                where DbFunctions.TruncateTime(req.SampleCreatedOn) == DbFunctions.TruncateTime(SampleDate)
                                && req.VisitType.ToLower() == "outpatient" && req.RunNumberType.ToLower() != "cyto" && req.RunNumberType.ToLower() != "histo"
                                group req by 1 into req
                                select new
                                {
                                    SampleCode = req.Max(a => a.SampleCode)
                                }).FirstOrDefault();

            //latestSampleCode is null if no sample codes are generated for the day.
            //Increment the latest samplecode by 1 to get the new samplecode of the day.
            if (latestSample != null)
            {
                newSampleSequence = (int)latestSample.SampleCode + 1;
            }


            return newSampleSequence;


        }


        //For RunNumber of Histo Type LabTests
        private int? GetYearlyTypeLatestSampleSequence(LabDbContext labDbContext, string RunNumType)
        {
            int? newSampleSequence = 1;

            var samplesByType = (from req in labDbContext.Requisitions
                                 where req.RunNumberType.ToLower() == RunNumType
                                 && (req.SampleCode.HasValue)
                                 select new
                                 {
                                     RequisitionId = req.RequisitionId,
                                     SampleDate = req.SampleCreatedOn,
                                     SampleCode = req.SampleCode
                                 }).ToList();


            var latestYearSampleCode = (from smpl in samplesByType
                                        where DanpheDateConvertor.ConvertEngToNepDate((DateTime)smpl.SampleDate).Year
                                            == DanpheDateConvertor.ConvertEngToNepDate(System.DateTime.Now).Year
                                        group smpl by 1 into req
                                        select new
                                        {
                                            SampleCode = req.Max(a => a.SampleCode)
                                        }).FirstOrDefault();

            if (latestYearSampleCode != null)
            {
                newSampleSequence = (int)latestYearSampleCode.SampleCode + 1;
            }


            return newSampleSequence;

        }


        private int? GetInpatientLatestSampleSequence(LabDbContext labDbContext)
        {
            int? newSampleSequence = 1;

            var samplesByType = (from req in labDbContext.Requisitions
                                 where (req.VisitType.ToLower() == "inpatient" || req.VisitType.ToLower() == "emergency") && (req.RunNumberType.ToLower() != "cyto") && (req.RunNumberType.ToLower() != "histo")
                                 && (req.SampleCode.HasValue)
                                 select new
                                 {
                                     RequisitionId = req.RequisitionId,
                                     SampleDate = req.SampleCreatedOn,
                                     SampleCode = req.SampleCode
                                 }).ToList();


            var latestYearSampleCode = (from smpl in samplesByType
                                        where DanpheDateConvertor.ConvertEngToNepDate((DateTime)smpl.SampleDate).Year
                                            == DanpheDateConvertor.ConvertEngToNepDate(System.DateTime.Now).Year
                                        group smpl by 1 into req
                                        select new
                                        {
                                            SampleCode = req.Max(a => a.SampleCode)
                                        }).FirstOrDefault();

            if (latestYearSampleCode != null)
            {
                newSampleSequence = (int)latestYearSampleCode.SampleCode + 1;
            }

            return newSampleSequence;

        }


        //ashim: 09Sep2018 : SampleCode Logic.
        public string GetSampleCodeFormatted(int? sampleCode, DateTime sampleCreatedOn, string visitType, string RunNumberType)
        {
            if (visitType.ToLower() == "outpatient")
            {
                if (RunNumberType.ToLower() == "normal")
                {
                    NepaliDateType nepDate = DanpheDateConvertor.ConvertEngToNepDate(sampleCreatedOn);
                    return sampleCode.ToString() + "/" + nepDate.Day.ToString();
                }
                else
                {
                    NepaliDateType nepDate = DanpheDateConvertor.ConvertEngToNepDate(sampleCreatedOn);
                    return sampleCode.ToString() + "/" + nepDate.Year.ToString().Substring(1, 3);
                }

            }
            else if (visitType.ToLower() == "inpatient" || visitType.ToLower() == "emergency")
            {
                if (RunNumberType.ToLower() == "normal")
                {
                    NepaliDateType nepDate = DanpheDateConvertor.ConvertEngToNepDate(sampleCreatedOn);
                    return sampleCode.ToString() + "/" + nepDate.Day.ToString();
                }
                else
                {
                    NepaliDateType nepDate = DanpheDateConvertor.ConvertEngToNepDate(sampleCreatedOn);
                    return sampleCode.ToString() + "/" + nepDate.Year.ToString().Substring(1, 3);
                }
            }
            else
            {
                return "Cannot Get Samplecode";
            }

        }

        //Suraj:06Sep2018 ReportTemplateId could be updated in case of html template.
        public void UpdateReportTemplateId(Int64 requisitionid, int? templateId, LabDbContext labDbContext, int currentUserId)
        {

            LabRequisitionModel labRequisition = (from req in labDbContext.Requisitions
                                                  where req.RequisitionId == requisitionid
                                                  select req).FirstOrDefault();
            labRequisition.ReportTemplateId = templateId ?? default(int);
            labRequisition.ModifiedBy = currentUserId;
            labRequisition.ModifiedOn = DateTime.Now;
            labDbContext.Entry(labRequisition).Property(a => a.ReportTemplateId).IsModified = true;
            labDbContext.Entry(labRequisition).Property(a => a.ModifiedBy).IsModified = true;
            labDbContext.Entry(labRequisition).Property(a => a.ModifiedOn).IsModified = true;
            labDbContext.SaveChanges();
        }

        //sud: 19Sept'18
        private void EditComponentsResults(LabDbContext dbContext, List<LabTestComponentResult> compsList, RbacUser currentUser)
        {
            if (compsList != null && compsList.Count > 0)
            {
                //to update Report Template ID on Requisition Table                
                Int64 reqId = compsList[0].RequisitionId;
                int? templateId = compsList[0].TemplateId;

                LabRequisitionModel LabRequisition = dbContext.Requisitions.Where(val => val.RequisitionId == reqId).FirstOrDefault();
                if (LabRequisition.ReportTemplateId != reqId)
                {
                    UpdateReportTemplateId(reqId, templateId, dbContext, currentUser.EmployeeId);
                }
                //Template ID Updated in Requisition Table


                //get distinct requisitionids, where we need to update or add components.
                List<Int64> distinctReqs = compsList.Select(c => c.RequisitionId).Distinct().ToList();



                foreach (var req in distinctReqs)
                {

                    //Section-1: Get/Filter All Components of Current Requisition from Databasea as well as from client.
                    List<LabTestComponentResult> requisitionsComps_Db = dbContext.LabTestComponentResults.Where(c => c.RequisitionId == req
                    && (c.IsActive.HasValue ? c.IsActive == true : true)).ToList();

                    List<LabTestComponentResult> reqsComps_Client = compsList.Where(c => c.RequisitionId == req).ToList();


                    //Section-2: Separate Client components in two groups: a. Newly Added, b. Updated
                    List<LabTestComponentResult> newlyAddedComps_Client = reqsComps_Client.Where(c => c.TestComponentResultId == 0).ToList();
                    List<LabTestComponentResult> updatedComps_Client = reqsComps_Client.Where(c => c.TestComponentResultId != 0).ToList();

                    ///Section-3: Create list of deleted components by checking if Component existing in db has come or not from Client.
                    List<LabTestComponentResult> deletedComps_Db = new List<LabTestComponentResult>();
                    //find better approach to get deleted components
                    foreach (var dbComp in requisitionsComps_Db)
                    {
                        //if component from db is not found in client's list then delete it. i.e: set IsActive=0
                        if (reqsComps_Client.Where(c => c.TestComponentResultId == dbComp.TestComponentResultId).Count() == 0)
                        {
                            deletedComps_Db.Add(dbComp);
                        }
                    }

                    //Section-4: Add new components to dbContext -- Don't save it yet, we'll do the Save action at the end.
                    if (newlyAddedComps_Client.Count > 0)
                    {
                        foreach (var c in newlyAddedComps_Client)
                        {
                            c.CreatedBy = currentUser.EmployeeId;
                            dbContext.LabTestComponentResults.Add(c);
                        }
                    }

                    //Section:5--Update values of dbComponent by that of ClientComponent--
                    //note that we have to update the peoperties of dbComponent, not ClientComponent.
                    //DON'T Save it YET.. we'll do that at the end..
                    if (updatedComps_Client.Count > 0)
                    {
                        foreach (var comp in updatedComps_Client)
                        {
                            LabTestComponentResult dbComp = requisitionsComps_Db.Where(c => c.TestComponentResultId == comp.TestComponentResultId).FirstOrDefault();
                            dbComp.Value = comp.Value;
                            dbComp.Range = comp.Range;
                            dbComp.RangeDescription = comp.RangeDescription;
                            dbComp.Remarks = comp.Remarks;
                            dbComp.IsAbnormal = comp.IsAbnormal;
                            dbComp.AbnormalType = comp.AbnormalType;
                            dbComp.ModifiedOn = DateTime.Now;
                            dbComp.ModifiedBy = currentUser.EmployeeId;
                            dbComp.IsActive = true;
                            dbComp.IsNegativeResult = comp.IsNegativeResult;

                            dbContext.Entry(dbComp).Property(a => a.Value).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.IsNegativeResult).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.Range).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.RangeDescription).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.IsAbnormal).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.AbnormalType).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.Remarks).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.ModifiedOn).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.ModifiedBy).IsModified = true;
                            dbContext.Entry(dbComp).Property(a => a.IsActive).IsModified = true;
                        }
                    }

                    //Section-5: Update IsActive Status of DeletedComponents.
                    if (deletedComps_Db.Count > 0)
                    {
                        foreach (var dbComp in deletedComps_Db)
                        {
                            dbComp.IsActive = false;
                            dbContext.Entry(dbComp).Property(a => a.IsActive).IsModified = true;
                        }
                    }

                }

                //YES: NOW After all Requisitions and Components are upated, we can call the SaveChanges Function()-- happy ?  :)
                dbContext.SaveChanges();

                //if (docPatPortalSync)
                //{
                //    DocPatPortalBL.PutLabReports(LabRequisition.LabReportId, distinctReqs, dbContext);
                //}

            }
        }


        private bool ValidatePrintOption(bool allowOutPatWithProv, string visittype, string billingstatus)
        {
            if (visittype.ToLower() == "inpatient")
            {
                return true;
            }
            else
            {
                if (allowOutPatWithProv)
                {
                    if (billingstatus.ToLower() == "paid" || billingstatus.ToLower() == "unpaid" || billingstatus.ToLower() == "provisional")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (billingstatus.ToLower() == "paid" || billingstatus.ToLower() == "unpaid")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }







        //Anish: 24 Nov'18 (Single Function that returns required AllLabData based on either BarCodeNumber or RunNumber and thus removes code redundancy)
        //private LabMasterModel AllLabDataFromRunNumOrBarCode(LabDbContext labDbContext, int barcodeNumber=0, string formattedCode="0/0")
        //{
        //    LabMasterModel LabMasterData = new LabMasterModel();


        //    string completeSampleCode = formattedCode;

        //    var mainCode = formattedCode.Split('/');
        //    int samplNumber = Convert.ToInt32(mainCode[0]);
        //    int code = Convert.ToInt32(mainCode[1]);

        //    if (formattedCode != "0/0")
        //    {               

        //        DateTime englishDateToday = DateTime.Now;
        //        NepaliDateType nepaliDate = DanpheDateConvertor.ConvertEngToNepDate(englishDateToday);

        //        if (code > 32)
        //        {
        //            nepaliDate.Year = 2000 + code;
        //        }
        //        else
        //        {
        //            nepaliDate.Day = code;
        //        }

        //        englishDateToday = DanpheDateConvertor.ConvertNepToEngDate(nepaliDate);

        //    }

        //    var reportWithHtmlTemplate = (from req in labDbContext.Requisitions
        //                                  join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
        //                                  join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
        //                                  join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
        //                                  where req.OrderStatus.ToLower() == "pending"
        //                                  && req.SampleCode != null
        //                                  && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
        //                                  && (barcodeNumber == 0 ? true : (req.BarCodeNumber == barcodeNumber))
        //                                  && (completeSampleCode == "0/0" ? true : (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false) )
        //                                  && (template.TemplateType.ToLower() == "html")

        //                                  group new { req, test, template } by new
        //                                  {
        //                                      patient,
        //                                      req.SampleCode,
        //                                      DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
        //                                      req.VisitType,
        //                                      req.RequisitionId,
        //                                      req.RunNumberType,
        //                                      req.BarCodeNumber,
        //                                      req.WardName
        //                                  } into grp
        //                                  select new LabPendingResultVM
        //                                  {
        //                                      PatientId = grp.Key.patient.PatientId,
        //                                      PatientCode = grp.Key.patient.PatientCode,
        //                                      DateOfBirth = grp.Key.patient.DateOfBirth,
        //                                      PhoneNumber = grp.Key.patient.PhoneNumber,
        //                                      Gender = grp.Key.patient.Gender,
        //                                      PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

        //                                      SampleCode = grp.Key.SampleCode,
        //                                      SampleDate = grp.Key.Value,
        //                                      VisitType = grp.Key.VisitType,
        //                                      RunNumType = grp.Key.RunNumberType,
        //                                      BarCodeNumber = grp.Key.BarCodeNumber,
        //                                      WardName = grp.Key.WardName,
        //                                      Tests = grp.Select(a =>
        //                                      new LabPendingResultVM.LabTestDetail()
        //                                      {
        //                                          RequisitionId = a.req.RequisitionId,
        //                                          TestName = a.test.LabTestName,
        //                                          LabTestId = a.test.LabTestId,
        //                                          ReportTemplateId = a.template.ReportTemplateID,
        //                                          ReportTemplateShortName = a.template.ReportTemplateShortName,
        //                                          RunNumberType = a.req.RunNumberType
        //                                      }).OrderBy(req => req.RequisitionId).ToList()
        //                                  }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();

        //    var reportWithNormalEntry = (from req in labDbContext.Requisitions
        //                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
        //                                 join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
        //                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
        //                                 where req.OrderStatus.ToLower() == "pending"
        //                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
        //                                 && (barcodeNumber == 0 ? true : (req.BarCodeNumber == barcodeNumber))
        //                                 && (completeSampleCode == "0/0" ? true : (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false))
        //                                 && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
        //                                 group new { req, test, template } by new
        //                                 {
        //                                     patient,
        //                                     req.SampleCode,
        //                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
        //                                     req.VisitType,
        //                                     req.RunNumberType,
        //                                     req.BarCodeNumber,
        //                                     req.WardName
        //                                 } into grp
        //                                 select new LabPendingResultVM
        //                                 {
        //                                     PatientId = grp.Key.patient.PatientId,
        //                                     PatientCode = grp.Key.patient.PatientCode,
        //                                     DateOfBirth = grp.Key.patient.DateOfBirth,
        //                                     PhoneNumber = grp.Key.patient.PhoneNumber,
        //                                     Gender = grp.Key.patient.Gender,
        //                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

        //                                     SampleCode = grp.Key.SampleCode,
        //                                     SampleDate = grp.Key.Value,
        //                                     VisitType = grp.Key.VisitType,
        //                                     RunNumType = grp.Key.RunNumberType,
        //                                     BarCodeNumber = grp.Key.BarCodeNumber,
        //                                     WardName = grp.Key.WardName,
        //                                     Tests = grp.Select(a =>
        //                                     new LabPendingResultVM.LabTestDetail()
        //                                     {
        //                                         RequisitionId = a.req.RequisitionId,
        //                                         TestName = a.test.LabTestName,
        //                                         LabTestId = a.test.LabTestId,
        //                                         ReportTemplateId = a.template.ReportTemplateID,
        //                                         ReportTemplateShortName = a.template.ReportTemplateShortName
        //                                     }).OrderBy(req => req.RequisitionId).ToList()
        //                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();



        //    foreach (var rep in reportWithHtmlTemplate)
        //    {
        //        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
        //        if(barcodeNumber != 0)
        //        {
        //            LabMasterData.AddResult.Add(rep);
        //        }
        //        else 
        //        {
        //            if(rep.SampleCodeFormatted == completeSampleCode)
        //            {
        //                LabMasterData.AddResult.Add(rep);
        //            }
        //        }

        //    }
        //    foreach (var repNormal in reportWithNormalEntry)
        //    {
        //        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
        //        if (barcodeNumber != 0)
        //        {
        //            LabMasterData.AddResult.Add(repNormal);
        //        }
        //        else
        //        {
        //            if (repNormal.SampleCodeFormatted == completeSampleCode)
        //            {
        //                LabMasterData.AddResult.Add(repNormal);
        //            }
        //        }
        //    }

        //    LabMasterData.AddResult = LabMasterData.AddResult.OrderByDescending(d => d.SampleDate).ThenByDescending(c => c.SampleCode).ToList();


        //    var pendingNormalReports = (from req in labDbContext.Requisitions
        //                                join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
        //                                join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
        //                                join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
        //                                where req.OrderStatus.ToLower() == "result-added"
        //                                && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
        //                                && (barcodeNumber == 0 ? true : (req.BarCodeNumber == barcodeNumber))
        //                                && (completeSampleCode == "0/0" ? true : (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false))
        //                                && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
        //                                group new { req, template, patient, test } by new
        //                                {
        //                                    patient,
        //                                    req.SampleCode,
        //                                    DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
        //                                    req.VisitType,
        //                                    req.RunNumberType,
        //                                    req.BarCodeNumber,
        //                                    req.WardName
        //                                } into grp
        //                                select new LabPendingResultVM
        //                                {
        //                                    PatientId = grp.Key.patient.PatientId,
        //                                    PatientCode = grp.Key.patient.PatientCode,
        //                                    DateOfBirth = grp.Key.patient.DateOfBirth,
        //                                    PhoneNumber = grp.Key.patient.PhoneNumber,
        //                                    Gender = grp.Key.patient.Gender,
        //                                    PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

        //                                    SampleCode = grp.Key.SampleCode,
        //                                    SampleDate = grp.Key.Value,
        //                                    VisitType = grp.Key.VisitType,
        //                                    RunNumType = grp.Key.RunNumberType,
        //                                    BarCodeNumber = grp.Key.BarCodeNumber,
        //                                    WardName = grp.Key.WardName,
        //                                    Tests = (from requisition in labDbContext.Requisitions
        //                                             join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
        //                                             join template in labDbContext.LabReportTemplates on requisition.ReportTemplateId equals template.ReportTemplateID
        //                                             where requisition.PatientId == grp.Key.patient.PatientId
        //                                            && requisition.SampleCode == grp.Key.SampleCode
        //                                            && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
        //                                            && requisition.OrderStatus.ToLower() == "result-added"
        //                                            && requisition.BillingStatus.ToLower() != "cancel" && requisition.BillingStatus.ToLower() != "returned"
        //                                            && (template.TemplateType.ToLower() == "normal" || template.TemplateType.ToLower() == "culture")
        //                                             select new LabPendingResultVM.LabTestDetail
        //                                             {
        //                                                 RequisitionId = requisition.RequisitionId,
        //                                                 LabTestId = requisition.LabTestId,
        //                                                 TestName = requisition.LabTestName

        //                                             }).Distinct().ToList()
        //                                }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);


        //    var pendingHtmlNCS = (from req in labDbContext.Requisitions
        //                          join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
        //                          join template in labDbContext.LabReportTemplates on req.ReportTemplateId equals template.ReportTemplateID
        //                          join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
        //                          where req.OrderStatus.ToLower() == "result-added"
        //                          && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
        //                          && (barcodeNumber == 0 ? true : (req.BarCodeNumber == barcodeNumber))
        //                          && (completeSampleCode == "0/0" ? true : (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false))
        //                          && (template.TemplateType.ToLower() == "html")
        //                          group new { req, template, patient, test } by new
        //                          {
        //                              patient,
        //                              req,
        //                              req.RunNumberType,
        //                              req.BarCodeNumber,
        //                              req.WardName
        //                          } into grp
        //                          select new LabPendingResultVM
        //                          {
        //                              PatientId = grp.Key.patient.PatientId,
        //                              PatientCode = grp.Key.patient.PatientCode,
        //                              DateOfBirth = grp.Key.patient.DateOfBirth,
        //                              PhoneNumber = grp.Key.patient.PhoneNumber,
        //                              Gender = grp.Key.patient.Gender,
        //                              PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

        //                              SampleCode = grp.Key.req.SampleCode,
        //                              SampleDate = DbFunctions.TruncateTime(grp.Key.req.SampleCreatedOn).Value,
        //                              VisitType = grp.Key.req.VisitType,
        //                              RunNumType = grp.Key.RunNumberType,
        //                              BarCodeNumber = grp.Key.BarCodeNumber,
        //                              WardName = grp.Key.WardName,
        //                              Tests = (from requisition in labDbContext.Requisitions
        //                                       join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
        //                                       where requisition.PatientId == grp.Key.patient.PatientId
        //                                        && requisition.RequisitionId == grp.Key.req.RequisitionId

        //                                       select new LabPendingResultVM.LabTestDetail
        //                                       {
        //                                           RequisitionId = requisition.RequisitionId,
        //                                           LabTestId = requisition.LabTestId,
        //                                           TestName = requisition.LabTestName
        //                                       }).ToList()
        //                          }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId);

        //    foreach (var rep in pendingHtmlNCS)
        //    {
        //        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
        //        if (barcodeNumber != 0)
        //        {
        //            LabMasterData.PendingReport.Add(rep);
        //        }
        //        else
        //        {
        //            if (rep.SampleCodeFormatted == completeSampleCode)
        //            {
        //                LabMasterData.PendingReport.Add(rep);
        //            }
        //        }
        //    }
        //    foreach (var repNormal in pendingNormalReports)
        //    {
        //        repNormal.SampleCodeFormatted = GetSampleCodeFormatted(repNormal.SampleCode, repNormal.SampleDate ?? default(DateTime), repNormal.VisitType, repNormal.RunNumType);
        //        if (barcodeNumber != 0)
        //        {
        //            LabMasterData.AddResult.Add(repNormal);
        //        }
        //        else
        //        {
        //            if (repNormal.SampleCodeFormatted == completeSampleCode)
        //            {
        //                LabMasterData.AddResult.Add(repNormal);
        //            }
        //        }
        //    }

        //    var FinalReport = (from req in labDbContext.Requisitions
        //                                 join report in labDbContext.LabReports on req.LabReportId equals report.LabReportId
        //                                 join test in labDbContext.LabTests on req.LabTestId equals test.LabTestId
        //                                 join patient in labDbContext.Patients on req.PatientId equals patient.PatientId
        //                                 where req.OrderStatus == "report-generated"
        //                                 && req.BillingStatus.ToLower() != "cancel" && req.BillingStatus.ToLower() != "returned"
        //                                 && (barcodeNumber == 0 ? true : (req.BarCodeNumber == barcodeNumber))
        //                                 && (completeSampleCode == "0/0" ? true : (req.SampleCode.HasValue ? (req.SampleCode == samplNumber) : false))
        //                                 group new { req, patient, test } by new
        //                                 {
        //                                     patient,
        //                                     req.SampleCode,
        //                                     req.LabReportId,
        //                                     DbFunctions.TruncateTime(req.SampleCreatedOn).Value,
        //                                     req.VisitType,
        //                                     req.RunNumberType,
        //                                     report.IsPrinted,
        //                                     req.BillingStatus,
        //                                     req.BarCodeNumber,
        //                                     req.WardName
        //                                 } into grp
        //                                 select new LabPendingResultVM
        //                                 {
        //                                     PatientId = grp.Key.patient.PatientId,
        //                                     PatientCode = grp.Key.patient.PatientCode,
        //                                     DateOfBirth = grp.Key.patient.DateOfBirth,
        //                                     PhoneNumber = grp.Key.patient.PhoneNumber,
        //                                     Gender = grp.Key.patient.Gender,
        //                                     PatientName = grp.Key.patient.FirstName + " " + (string.IsNullOrEmpty(grp.Key.patient.MiddleName) ? "" : grp.Key.patient.MiddleName + " ") + grp.Key.patient.LastName,

        //                                     SampleCode = grp.Key.SampleCode,
        //                                     SampleDate = grp.Key.Value,
        //                                     SampleCodeFormatted = "",
        //                                     VisitType = grp.Key.VisitType,
        //                                     RunNumType = grp.Key.RunNumberType,
        //                                     IsPrinted = grp.Key.IsPrinted,
        //                                     BillingStatus = grp.Key.BillingStatus,
        //                                     BarCodeNumber = grp.Key.BarCodeNumber,
        //                                     WardName = grp.Key.WardName,
        //                                     Tests = (from requisition in labDbContext.Requisitions
        //                                              join test in labDbContext.LabTests on requisition.LabTestId equals test.LabTestId
        //                                              where requisition.PatientId == grp.Key.patient.PatientId
        //                                              && requisition.SampleCode == grp.Key.SampleCode
        //                                              && DbFunctions.TruncateTime(requisition.SampleCreatedOn).Value == grp.Key.Value
        //                                              && requisition.OrderStatus == "report-generated"
        //                                              && requisition.LabReportId == grp.Key.LabReportId

        //                                              select new LabPendingResultVM.LabTestDetail
        //                                              {
        //                                                  RequisitionId = requisition.RequisitionId,
        //                                                  LabTestId = requisition.LabTestId,
        //                                                  TestName = requisition.LabTestName                                                          
        //                                              }).ToList()
        //                                 }).OrderByDescending(d => d.SampleDate).ThenByDescending(x => x.SampleCode).ThenByDescending(a => a.PatientId).ToList();

        //    foreach (var rep in FinalReport)
        //    {
        //        rep.SampleCodeFormatted = GetSampleCodeFormatted(rep.SampleCode, rep.SampleDate ?? default(DateTime), rep.VisitType, rep.RunNumType);
        //        if (barcodeNumber != 0)
        //        {
        //            LabMasterData.FinalReport.Add(rep);
        //        }
        //        else
        //        {
        //            if (rep.SampleCodeFormatted == completeSampleCode)
        //            {
        //                LabMasterData.FinalReport.Add(rep);
        //            }
        //        }
        //    }

        //    return LabMasterData;
        //}

    }
}
