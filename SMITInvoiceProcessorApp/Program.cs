using SMITInvoiceProcessorApp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace SchedulerJob
{
    class Program
    {
        static void Main(string[] args)
        {
            //Logger.Info("Job Started");

            try
            {
                // =========================
                // LOAD CONFIG
                // =========================
                bool configOk = AppConfig.Load();

                if (!configOk)
                {
                    Logger.Error("Config load failed. Job stopped.");
                    return;
                }

                //Logger.Info("Config loaded successfully");

                // =========================
                // DB TEST
                // =========================
                //TestDatabase();

                // =========================
                // MAIN JOB
                // =========================
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        ProcessFiles();

                //Logger.Info("Job Finished Successfully");
            }
            catch (Exception ex)
            {
                // absolutely never crash silently
                Logger.Error("Unexpected error in Main");
                Logger.Error(ex);
            }
        }

        private static void TestDatabase()
        {
            try
            {
                using (SqlConnection con =
                    new SqlConnection(AppConfig.ConnectionString))
                {
                    con.Open();

                    using (SqlCommand cmd =
                        new SqlCommand("SELECT GETDATE()", con))
                    {
                        var result = cmd.ExecuteScalar();
                        Logger.Info("DB Connected: " + result);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Database connection failed");
                Logger.Error(ex);
            }
        }

        private static void ProcessFiles()
        {
            try
            {
                //Logger.Info("ScanFolder: " + AppConfig.ScanFolder);
                //Logger.Info("ExFiles: " + AppConfig.ScanFolderExFiles);
                //Logger.Info("DropFolder: " + AppConfig.DropFolder);
                string ConnectionString = AppConfig.ConnectionString;
                string ScanFolder = AppConfig.ScanFolder;
                string ScanFolderExFiles = AppConfig.ScanFolderExFiles;
                string DropFolder = AppConfig.DropFolder;
                string PDFDropFolder = AppConfig.PDFDropFolder;
                string SpecialPDFDropFolder = AppConfig.SpecialPDFDropFolder;
                bool shouldTransferToSpecialFolder = false;
                List<string> fileTransferRuleTexts = GetFileTransferRules(ConnectionString);
                // TODO: your real logic here
                string[] xmlFiles = null;
                try
                {
                    xmlFiles = Directory.GetFiles(ScanFolder, "*.xml");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                if (!xmlFiles.Any())
                {
                    return;
                }
                foreach (var xmlFilePath in xmlFiles)
                {
                    string xmlFileName = "";
                    string pdfFileName = "";
                    string pdfFilePath = "";
                    string jsonText = "";

                    pdfFileName = Path.GetFileNameWithoutExtension(xmlFilePath);

                    try
                    {
                        if (!pdfFileName.Contains(".PDF"))
                        {
                            pdfFileName = pdfFileName + ".pdf";
                        }
                    }
                    catch (Exception)
                    {
                        pdfFileName = Path.GetFileNameWithoutExtension(xmlFilePath);
                    }
                    pdfFilePath = Path.Combine(Path.GetDirectoryName(xmlFilePath), pdfFileName);

                    xmlFileName = Path.GetFileNameWithoutExtension(pdfFileName) + ".xml";

                    SMITFileUploadLog log = new SMITFileUploadLog();
                    log.FileName = xmlFileName;
                    log.FileType = ".xml";
                    log.UploadedFrom = "OP-Scheduler";
                    log.AddedBy = "Mis";
                    log.DateAdded = DateTime.Now;
                    log.UpdatedBy = null;
                    log.DateUpdated = null;
                    log.DeletedBy = null;
                    log.IsDeleted = false;
                    log.DateDeleted = null;

                    string transferPath = DropFolder;
                    string transferPDFPath = PDFDropFolder;
                    var transactionList = new List<Transaction>();
                    try
                    {
                        jsonText = XmlToJsonConverter.ConvertXmlToJson(xmlFilePath);
                        log.FileDetailsJson = jsonText;
                        transactionList = ParseTransactions(xmlFilePath);
                        if (transactionList.Count() > 0)
                        {
                            foreach (var transaction in transactionList)
                            {
                                shouldTransferToSpecialFolder = fileTransferRuleTexts.Any(rule => transaction.RefNumber.ToLower().Contains(rule.ToLower()));

                                DbResult dbResult = new DbResult();
                                log.Status = "UPLOADED | In Print Queue";
                                transaction.GSTAmount = transaction.GSTAmount == "" ? "0" : transaction.GSTAmount;
                                transaction.Amount = transaction.Amount == "" ? "0" : transaction.Amount;
                                dbResult = InsertTransaction(
                                    transaction.OwnersCorporation,
                                    transaction.DRItemType,
                                    transaction.DRItem,
                                    transaction.GSTAmount,
                                    transaction.Description,
                                    transaction.TranDate,
                                    transaction.DueDate,
                                    transaction.DateEntered,
                                    transaction.Suppress,
                                    transaction.RefNumber,
                                    transaction.DRAccount,
                                    transaction.CRAccount,
                                    transaction.CRItem,
                                    transaction.Amount,
                                    transaction.TranType,
                                    transaction.Status,
                                    transaction.BPAYCRN,
                                    ConnectionString
                                );

                                if (shouldTransferToSpecialFolder)
                                {
                                    transferPDFPath = SpecialPDFDropFolder;
                                }

                                log.ErrorMessage = dbResult.StatusMessage;

                                // Map status codes to log messages and statuses
                                var statusMap = new Dictionary<int, string>
                                    {
                                        { -99, "SQL Error" },
                                        { -1,  "Data Missing/Owners Corporation not found" },
                                        { -2,  "Invalid Status/DR or CR Account not found" },
                                        { -10,  "Header insert failed" },
                                        { -11,  "Transaction ID not generated" },
                                        { -12,  "Header update failed" },
                                        { -13,  "DR entry insert failed" },
                                        { -14,  "CR entry insert failed" },
                                        { -15,  "Duplicate transaction found" },
                                        {  0,  "Data Not Found" }
                                    };

                                if (statusMap.ContainsKey(dbResult.StatusCode))
                                {
                                    if (dbResult.StatusCode == -99)
                                    {
                                        log.Status = dbResult.StatusMessage;
                                    }
                                    else
                                    {
                                        log.Status = statusMap[dbResult.StatusCode];
                                    }
                                    transferPath = ScanFolderExFiles;
                                    transferPDFPath = ScanFolderExFiles;
                                    Logger.Info(dbResult.StatusMessage, "WARNING");
                                }
                                else
                                {
                                    Logger.Info(dbResult.StatusMessage, "INFO");
                                }
                            }
                        }
                        else
                        {
                            log.Status = "UNSUPPORTED FILE FORMAT";
                            transferPath = ScanFolderExFiles;
                            transferPDFPath = ScanFolderExFiles;
                            Logger.Info("File format does not match the provided format", "WARNING");
                            log.ErrorMessage = $"Warning: File format does not match the provided format";
                        }
                    }
                    catch (Exception ex)
                    {
                        transferPath = ScanFolderExFiles;
                        transferPDFPath = ScanFolderExFiles;
                        Logger.Error(ex);
                        log.Status = "UPLOAD FAILED";
                        log.ErrorMessage = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    }

                    int result = SaveSMITFileUploadLog(log, ConnectionString);
                    if (result > 0)
                    {
                        #region Move Files
                        CopyFileSafe(pdfFilePath, Path.Combine(transferPDFPath, pdfFileName));
                        Logger.Info("Copy " + pdfFilePath + " to " + transferPDFPath);
                        CopyFileSafe(xmlFilePath, Path.Combine(transferPath, xmlFileName));
                        Logger.Info("Copy " + xmlFilePath + " to " + transferPath);

                        DeleteFileSafe(pdfFilePath);
                        Logger.Info("Delete " + pdfFilePath);
                        DeleteFileSafe(xmlFilePath);
                        Logger.Info("Delete " + xmlFilePath);
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("File processing failed");
                Logger.Error(ex);
            }
        }
        private static List<Transaction> ParseTransactions(string xmlFilePath)
        {
            //XDocument doc = XDocument.Load(xmlFilePath);
            string rawXml = File.ReadAllText(xmlFilePath);
            string safeXml = SanitizeXmlContent(rawXml);

            XDocument doc = XDocument.Parse(safeXml); // Load from string, not file path

            return doc.Descendants("Transaction")
                .Select(element => new Transaction
                {
                    OwnersCorporation = element.Attribute("OwnersCorporation")?.Value,
                    DRItemType = element.Attribute("DRItemType")?.Value,
                    DRItem = element.Attribute("DRItem")?.Value,
                    GSTAmount = element.Attribute("GSTAmount")?.Value,
                    Description = element.Attribute("Description")?.Value,
                    DateEntered = element.Attribute("DateEntered")?.Value,
                    Suppress = element.Attribute("Suppress")?.Value,
                    DueDate = element.Attribute("DueDate")?.Value,
                    RefNumber = element.Attribute("RefNumber")?.Value,
                    DRAccount = element.Attribute("DRAccount")?.Value,
                    CRAccount = element.Attribute("CRAccount")?.Value,
                    CRItem = element.Attribute("CRItem")?.Value,
                    CRItemType = element.Attribute("CRItemType")?.Value,
                    Amount = element.Attribute("Amount")?.Value,
                    TranDate = element.Attribute("TranDate")?.Value,
                    TranType = element.Attribute("TranType")?.Value,
                    Status = element.Attribute("Status")?.Value,
                    BPAYCRN = element.Attribute("BPAYCRN")?.Value
                })
                .ToList();
        }
        private static string SanitizeXmlContent(string xml)
        {
            // Regex to find element values and attribute values
            return Regex.Replace(xml, @">(.*?)<", match =>
            {
                string content = match.Groups[1].Value;

                // Skip empty content
                if (string.IsNullOrWhiteSpace(content)) return match.Value;

                // Skip content that is already a valid XML entity
                content = content
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");

                return $">{content}<";
            });
        }
        private static void CopyFileSafe(string sourcePath, string destPath)
        {
            try
            {
                System.IO.File.Copy(sourcePath, destPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        private static bool DeleteFileSafe(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }
        private static string ExtractParts(string filename)
        {
            // Pattern 1: Matches 3407-11080-101102 (digits with dashes)
            var match1 = Regex.Match(filename, @"^(\d+)-(\d+)-(\d+)_");

            if (match1.Success)
            {
                return $"1,{match1.Groups[1].Value},{match1.Groups[2].Value},{match1.Groups[3].Value}";
            }
            else
            {
                // Pattern 2: Matches 1683_myBins_<any>_INV - 124747_ (capture 1683, myBins, and invoice number)
                var match2 = Regex.Match(filename, @"^(\d+)_([a-zA-Z0-9]+)_[^_]+_INV\s*-\s*(\d+)_");

                if (match2.Success)
                {
                    return $"2,{match2.Groups[1].Value},{match2.Groups[2].Value},{match2.Groups[3].Value}";
                }
            }
            return "N/A";
        }
        private static int SaveSMITFileUploadLog(SMITFileUploadLog log,string connectionString, int maxRetries = 3, int delayMs = 500)
        {
            string query = @"
                            INSERT INTO SMITFileUploadLog
                            (FileName, FileType, Status, FileDetailsJson, ErrorMessage, UploadedFrom,
                             AddedBy, DateAdded, IsDeleted, UpdatedBy, DateUpdated, DeletedBy, DateDeleted)
                            VALUES
                            (@FileName, @FileType, @Status, @FileDetailsJson, @ErrorMessage, @UploadedFrom,
                             @AddedBy, @DateAdded, @IsDeleted, @UpdatedBy, @DateUpdated, @DeletedBy, @DateDeleted);
                            
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FileName", log.FileName);
                        cmd.Parameters.AddWithValue("@FileType", log.FileType);
                        cmd.Parameters.AddWithValue("@Status", log.Status);
                        cmd.Parameters.AddWithValue("@FileDetailsJson", log.FileDetailsJson);
                        cmd.Parameters.AddWithValue("@ErrorMessage", log.ErrorMessage);
                        cmd.Parameters.AddWithValue("@UploadedFrom", log.UploadedFrom);
                        cmd.Parameters.AddWithValue("@AddedBy", log.AddedBy);
                        cmd.Parameters.AddWithValue("@DateAdded", log.DateAdded.ToString("yyyy-MM-dd HH:ss"));
                        cmd.Parameters.AddWithValue("@IsDeleted", log.IsDeleted);
                        cmd.Parameters.AddWithValue("@UpdatedBy", (object)log.UpdatedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateUpdated", (object)log.DateUpdated ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DeletedBy", (object)log.DeletedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateDeleted", (object)log.DateDeleted ?? DBNull.Value);

                        if (conn.State != ConnectionState.Open)
                            conn.Open();
                        object result = cmd.ExecuteScalar();

                        if (result != null && int.TryParse(result.ToString(), out int insertedId))
                        {
                            return insertedId;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                catch (SqlException ex) when (IsTransient(ex))
                {
                    attempt++;
                    Logger.Info($"Transient SQL error, attempt {attempt} of {maxRetries}: {ex.Message}");
                    Thread.Sleep(delayMs); // wait before retrying
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return -1;
                }
            }

            return 0; // failed after all retries
        }
        private static bool IsTransient(SqlException ex)
        {
            // Typical transient error numbers in SQL Server
            int[] transientErrors = { 4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920 };

            return transientErrors.Contains(ex.Number);
        }
        public static DbResult InsertTransaction(
        string ownersCorporation,
        string drItemType,
        string drItem,
        string gSTAmount,
        string description,
        string tranDate,
        string dueDate,
        string dateEntered,
        string suppress,
        string refNumber,
        string drAccount,
        string crAccount,
        string crItem,
        string amount,
        string tranType,
        string status,
        string bpayCrn,
        string connectionString,
        int maxRetries = 3,
        int delayMs = 500)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_Tran_Insert", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // ======================
                        // INPUT PARAMETERS
                        // ======================
                        cmd.Parameters.Add("@OwnersCorporation", SqlDbType.VarChar, 50).Value = ownersCorporation;
                        cmd.Parameters.Add("@DRItemType", SqlDbType.VarChar, 50).Value = drItemType;
                        cmd.Parameters.Add("@DRItem", SqlDbType.VarChar, 50).Value = drItem;
                        cmd.Parameters.Add("@GSTAmount", SqlDbType.Decimal).Value = gSTAmount;
                        cmd.Parameters.Add("@Description", SqlDbType.VarChar, 255).Value = description;
                        cmd.Parameters.Add("@TranDate", SqlDbType.DateTime).Value = tranDate;
                        cmd.Parameters.Add("@DueDate", SqlDbType.DateTime).Value = dueDate;

                        cmd.Parameters.Add("@DateEntered", SqlDbType.DateTime).Value =
                            string.IsNullOrEmpty(dateEntered) ? (object)DBNull.Value : dateEntered;

                        cmd.Parameters.Add("@Suppress", SqlDbType.VarChar, 10).Value = suppress;
                        cmd.Parameters.Add("@RefNumber", SqlDbType.VarChar, 50).Value = refNumber;
                        cmd.Parameters.Add("@DRAccount", SqlDbType.VarChar, 255).Value = drAccount;
                        cmd.Parameters.Add("@CRAccount", SqlDbType.VarChar, 255).Value = crAccount;
                        cmd.Parameters.Add("@CRItem", SqlDbType.VarChar, 50).Value = crItem;

                        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
                        cmd.Parameters.Add("@TranType", SqlDbType.VarChar, 10).Value = tranType;
                        cmd.Parameters.Add("@Status", SqlDbType.VarChar, 10).Value = status;
                        cmd.Parameters.Add("@BPAYCRN", SqlDbType.VarChar, 50).Value = bpayCrn;

                        // ======================
                        // OUTPUT PARAMETERS
                        // ======================
                        cmd.Parameters.Add(new SqlParameter("@StatusCode", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        });

                        cmd.Parameters.Add(new SqlParameter("@StatusMsg", SqlDbType.VarChar, 300)
                        {
                            Direction = ParameterDirection.Output
                        });

                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        cmd.ExecuteNonQuery();

                        return new DbResult
                        {
                            StatusCode = (int)cmd.Parameters["@StatusCode"].Value,
                            StatusMessage = cmd.Parameters["@StatusMsg"].Value.ToString()
                        };
                    }
                }
                catch (SqlException ex) when (IsTransient(ex))
                {
                    attempt++;

                    Logger.Info(
                        $"Transient SQL error in InsertTransaction, attempt {attempt} of {maxRetries}: {ex.Message}"
                    );

                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);

                    return new DbResult
                    {
                        StatusCode = -99,
                        StatusMessage = "Unhandled exception occurred."
                    };
                }
            }

            return new DbResult
            {
                StatusCode = 0,
                StatusMessage = "Operation failed after maximum retries."
            };
        }
        private static List<string> GetFileTransferRules(string connectionString)
        {
            List<string> rules = new List<string>();

            string query = @"SELECT [Text] FROM FileTransferRuleText ORDER BY ID";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rules.Add(reader["Text"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return rules;
        }
    }
}