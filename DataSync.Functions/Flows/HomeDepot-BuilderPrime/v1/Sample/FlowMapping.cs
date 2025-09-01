namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1.Sample;

public class FlowMapping
{
    public static string SampleFlowMapping()
    {
        return """
                 {
                   "map": {
                     "Acknowledged": [
                       "New Leads",
                       "Lead Issued"
                     ],
                     "Confirmed": [
                       "Call Backs",
                       "Appointment Set",
                       "Appointment Confirmed",
                       "Estimate Sent",
                       "Appointment Not Set 2-7",
                       "Appointment Not Set 7-30",
                       "Appointment Not Set 30...",
                       "Appointment Not Set 60+",
                       "Appt Cxl (Reset)",
                       "Appt Cxl (Reset) 30",
                       "Appt Cxl (Reset) 60+",
                       "Demo"
                     ],
                     "Sold": [
                       "Job Sold",
                       "Customer",
                       "Job In Progress"
                     ],
                     "Cancelled": [
                       "Job Sold & Cancelled",
                       "Demo / No Sale",
                       "No Demo",
                       "Bad Data",
                       "Out Of Area",
                       "Credit Declined"
                     ]
                   }
                 }
                 """;
    }
}