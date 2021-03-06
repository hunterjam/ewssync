﻿/*
 * By David Barrett, Microsoft Ltd. 2014. Use at your own risk.  No warranties are given.
 * 
 * DISCLAIMER:
 * THIS CODE IS SAMPLE CODE. THESE SAMPLES ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
 * MICROSOFT FURTHER DISCLAIMS ALL IMPLIED WARRANTIES INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OF MERCHANTABILITY OR OF FITNESS FOR
 * A PARTICULAR PURPOSE. THE ENTIRE RISK ARISING OUT OF THE USE OR PERFORMANCE OF THE SAMPLES REMAINS WITH YOU. IN NO EVENT SHALL
 * MICROSOFT OR ITS SUPPLIERS BE LIABLE FOR ANY DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF BUSINESS PROFITS,
 * BUSINESS INTERRUPTION, LOSS OF BUSINESS INFORMATION, OR OTHER PECUNIARY LOSS) ARISING OUT OF THE USE OF OR INABILITY TO USE THE
 * SAMPLES, EVEN IF MICROSOFT HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES. BECAUSE SOME STATES DO NOT ALLOW THE EXCLUSION OR LIMITATION
 * OF LIABILITY FOR CONSEQUENTIAL OR INCIDENTAL DAMAGES, THE ABOVE LIMITATION MAY NOT APPLY TO YOU.
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.WebServices.Autodiscover;
using Microsoft.Exchange.WebServices.Data;

namespace EWSStreamingNotificationSample
{
    public class Mailboxes
    {
        // This class handles the autodiscover and exposes information needed for grouping

        private Dictionary<string, MailboxInfo> _mailboxes;  // Stores information for each mailbox, as returned by autodiscover
        private AutodiscoverService _autodiscover;
        private ClassLogger _logger;
        

        public Mailboxes(WebCredentials AutodiscoverCredentials, ClassLogger Logger, ITraceListener TraceListener = null)
        {
            _logger = Logger;
            _mailboxes = new Dictionary<string, MailboxInfo>();
            _autodiscover = new AutodiscoverService(ExchangeVersion.Exchange2013);  // Minimum version we need is 2013
            _autodiscover.RedirectionUrlValidationCallback = RedirectionCallback;
            if (TraceListener != null)
            {
                _autodiscover.TraceListener = TraceListener;
                _autodiscover.TraceFlags = TraceFlags.All;
                _autodiscover.TraceEnabled = true;
            }
            if (!(AutodiscoverCredentials==null))
                _autodiscover.Credentials=AutodiscoverCredentials;
        }

        static bool RedirectionCallback(string url)
        {
            return url.ToLower().StartsWith("https://");
        }

        public WebCredentials Credentials
        {
            set { _autodiscover.Credentials = value; }
        }

        public List<string> AllMailboxes
        {
            get
            {
                return _mailboxes.Keys.ToList<string>();
            }
        }



        public bool AddMailbox(string SMTPAddress)
        {
            // Perform autodiscover for the mailbox and store the information

            if (_mailboxes.ContainsKey(SMTPAddress))
            {
                // We already have autodiscover information for this mailbox, if it is recent enough we don't bother retrieving it again
                if (_mailboxes[SMTPAddress].IsStale)
                {
                    _mailboxes.Remove(SMTPAddress);
                }
                else
                    return true;
            }

            // Retrieve the autodiscover information
            GetUserSettingsResponse userSettings = null;
            try
            {
                userSettings = GetUserSettings(SMTPAddress);
            }
            catch (Exception ex)
            {
                _logger.Log(String.Format("Failed to autodiscover for {0}: {1}", SMTPAddress, ex.Message));
                return false;
            }

            // Store the autodiscover result, and check that we have what we need for subscriptions
            MailboxInfo info = new MailboxInfo(SMTPAddress, userSettings);
            if (!info.HaveSubscriptionInformation)
            {
                _logger.Log(String.Format("Autodiscover succeeded, but EWS Url was not returned for {0}", SMTPAddress));
                return false;
            }

            // Add the mailbox to our list, and if it will be part of a new group add that to the group list (with this mailbox as the primary mailbox)
            _mailboxes.Add(info.SMTPAddress, info);
            return true;
        }

        private GetUserSettingsResponse GetUserSettings(string Mailbox)
        {
            // Attempt autodiscover, with maximum of 10 hops
            // As per MSDN: http://msdn.microsoft.com/en-us/library/office/microsoft.exchange.webservices.autodiscover.autodiscoverservice.getusersettings(v=exchg.80).aspx

            Uri url = null;
            GetUserSettingsResponse response = null;
            

            for (int attempt = 0; attempt < 10; attempt++)
            {
                _autodiscover.Url = url;
                _autodiscover.EnableScpLookup = (attempt < 2);

                response = _autodiscover.GetUserSettings(Mailbox, UserSettingName.InternalEwsUrl, UserSettingName.ExternalEwsUrl, UserSettingName.GroupingInformation);

                if (response.ErrorCode == AutodiscoverErrorCode.RedirectAddress)
                {
                    return GetUserSettings(response.RedirectTarget);
                }
                else if (response.ErrorCode == AutodiscoverErrorCode.RedirectUrl)
                {
                    url = new Uri(response.RedirectTarget);
                }
                else
                {
                    return response;
                }
            }

            throw new Exception("No suitable Autodiscover endpoint was found.");
        }

        public MailboxInfo Mailbox(string SMTPAddress)
        {
            if (_mailboxes.ContainsKey(SMTPAddress))
                return _mailboxes[SMTPAddress];
            return null;
        }
    }
}
