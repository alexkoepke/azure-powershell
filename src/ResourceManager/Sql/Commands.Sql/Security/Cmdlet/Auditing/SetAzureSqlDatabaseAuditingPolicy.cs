﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Sql.Properties;
using Microsoft.Azure.Commands.Sql.Security.Model;
using Microsoft.Azure.Commands.Sql.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.Azure.Commands.Sql.Services;

namespace Microsoft.Azure.Commands.Sql.Security.Cmdlet.Auditing
{
    /// <summary>
    /// Sets the auditing policy properties for a specific database.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureSqlDatabaseAuditingPolicy"), OutputType(typeof(DatabaseAuditingPolicyModel))]
    public class SetAzureSqlDatabaseAuditingPolicy : SqlDatabaseAuditingCmdletBase
    {
        /// <summary>
        ///  Defines whether the cmdlets will output the model object at the end of its execution
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Gets or sets the names of the event types to use.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Event types to audit")]
        [ValidateSet(SecurityConstants.DeprecatedAuditEvents.DataAccess, SecurityConstants.DeprecatedAuditEvents.SchemaChanges, SecurityConstants.DeprecatedAuditEvents.DataChanges, SecurityConstants.DeprecatedAuditEvents.SecurityExceptions, SecurityConstants.DeprecatedAuditEvents.RevokePermissions, SecurityConstants.PlainSQL_Success, SecurityConstants.PlainSQL_Failure, SecurityConstants.ParameterizedSQL_Success, SecurityConstants.ParameterizedSQL_Failure, SecurityConstants.StoredProcedure_Success, SecurityConstants.StoredProcedure_Failure, SecurityConstants.Login_Success, SecurityConstants.Login_Failure, SecurityConstants.TransactionManagement_Success, SecurityConstants.TransactionManagement_Failure, SecurityConstants.All, SecurityConstants.None, IgnoreCase = false)]
        public string[] EventType { get; set; }

        /// <summary>
        /// Gets or sets the name of the storage account to use.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the storage account")]
        [ValidateNotNullOrEmpty]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the name of the storage account to use.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The type of the storage key")]
        [ValidateSet(SecurityConstants.Primary, SecurityConstants.Secondary, IgnoreCase = false)]
        [ValidateNotNullOrEmpty]
        public string StorageKeyType { get; set; }

        /// <summary>
        /// Gets or sets the number of retention days for the audit logs table.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The number of retention days for the audit logs table")]
        [ValidateNotNullOrEmpty]
        public uint? RetentionInDays { get; internal set; }

        /// <summary>
        /// Gets or sets the name of the audit logs table.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The name of the audit logs table")]
        [ValidateNotNullOrEmpty]
        public string TableIdentifier { get; internal set; }

        /// <summary>
        /// Returns true if the model object that was constructed by this cmdlet should be written out
        /// </summary>
        /// <returns>True if the model object should be written out, False otherwise</returns>
        protected override bool WriteResult() { return PassThru; }

        /// <summary>
        /// Updates the given model element with the cmdlet specific operation 
        /// </summary>
        /// <param name="model">A model object</param>
        protected override DatabaseAuditingPolicyModel ApplyUserInputToModel(DatabaseAuditingPolicyModel model)
        {
            base.ApplyUserInputToModel(model);
            AuditStateType orgAuditStateType = model.AuditState;
            model.AuditState = AuditStateType.Enabled;
            model.UseServerDefault = UseServerDefaultOptions.Disabled;
            if (StorageAccountName != null)
            {
                model.StorageAccountName = StorageAccountName;
            }
            if (!string.IsNullOrEmpty(StorageKeyType)) // the user enter a key type - we use it (and running over the previously defined key type)
            {
                model.StorageKeyType = (StorageKeyType == SecurityConstants.Primary) ? StorageKeyKind.Primary : StorageKeyKind.Secondary;
            }

            EventType = Util.ProcessAuditEvents(EventType);

            if (EventType != null) // the user provided event types to audit
            {
                model.EventType = EventType.Select(s => SecurityConstants.AuditEventsToAuditEventType[s]).ToArray();
            }

            if (RetentionInDays != null)
            {
                model.RetentionInDays = RetentionInDays;
            }

            if (TableIdentifier == null)
            {
                if ((orgAuditStateType == AuditStateType.New) && (model.RetentionInDays > 0))
                {
                    // If retention days is greater than 0 and no audit table identifier is supplied , we throw exception giving the user hint on the recommended TableIdentifier we got from the CSM
                    throw new Exception(string.Format(Resources.InvalidRetentionTypeSet, model.TableIdentifier));
                }
            }
            else
            {
                model.TableIdentifier = TableIdentifier;
            }

            if (Util.DeprecatedEventTypeFound(EventType))
            {
                WriteWarning(string.Format(Resources.DeprecatedEventTypeUsed));
            }
            return model;
        }
    }
}