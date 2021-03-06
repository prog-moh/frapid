﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using Frapid.ApplicationState.Cache;
using Frapid.Configuration;
using Frapid.Configuration.Db;
using Frapid.DataAccess;
using Frapid.DataAccess.Models;
using Frapid.DbPolicy;
using Frapid.Mapper;
using Frapid.Mapper.Database;
using Frapid.Mapper.Extensions;
using Frapid.Mapper.Query.NonQuery;
using Frapid.Mapper.Query.Select;
using Serilog;

namespace Frapid.WebApi.DataAccess
{
    public class FormRepository : DbAccess, IFormRepository
    {
        private const int PageSize = 10;

        public FormRepository(string schemaName, string tableName, string database, long loginId, int userId)
        {
            var me = AppUsers.GetCurrentAsync().Result;

            this._ObjectNamespace = Sanitizer.SanitizeIdentifierName(schemaName);
            this._ObjectName = Sanitizer.SanitizeIdentifierName(tableName.Replace("-", "_"));
            this.LoginId = me.LoginId;
            this.OfficeId = me.OfficeId;
            this.UserId = me.UserId;
            this.Database = database;
            this.LoginId = loginId;
            this.UserId = userId;

            if (!string.IsNullOrWhiteSpace(this._ObjectNamespace) &&
                !string.IsNullOrWhiteSpace(this._ObjectName))
            {
                this.FullyQualifiedObjectName = this._ObjectNamespace + "." + this._ObjectName;
                this.PrimaryKey = this.GetCandidateKey();
                this.LookupField = this.GetLookupField();
                this.NameColumn = this.GetNameColumn();
                this.IsValid = true;
            }
        }

        public sealed override string _ObjectNamespace { get; }
        public sealed override string _ObjectName { get; }
        public string FullyQualifiedObjectName { get; set; }
        public string PrimaryKey { get; set; }
        public string LookupField { get; set; }
        public string NameColumn { get; set; }
        public string Database { get; set; }
        public int UserId { get; set; }
        public bool IsValid { get; set; }
        public long LoginId { get; set; }
        public int OfficeId { get; set; }

        public async Task<long> CountAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return 0;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to count entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql = $"SELECT COUNT(*) FROM {this.FullyQualifiedObjectName} WHERE DELETED = @0;";
            return await Factory.ScalarAsync<long>(this.Database, sql, false).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetAllAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.ExportData, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the export entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql = $"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0";
            if (!string.IsNullOrWhiteSpace(this.PrimaryKey))
            {
                sql += $" ORDER BY {this.PrimaryKey};";
            }

            return await Factory.GetAsync<dynamic>(this.Database, sql, false).ConfigureAwait(false);
        }

        public async Task<dynamic> GetAsync(object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the get entity \"{this.FullyQualifiedObjectName}\" filtered by \"{this.PrimaryKey}\" with value {primaryKey} was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            if (string.IsNullOrWhiteSpace(this.PrimaryKey))
            {
                return null;
            }


            string sql = $"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0 AND {this.PrimaryKey}=@1;";
            return
                (await Factory.GetAsync<dynamic>(this.Database, sql, false, primaryKey).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<dynamic> GetFirstAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the get the first record of entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0", false);
            sql.OrderBy(this.PrimaryKey);
            sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), 0);
            sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), 1);

            return (await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<dynamic> GetPreviousAsync(object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the get the previous entity of \"{this.FullyQualifiedObjectName}\" by \"{this.PrimaryKey}\" with value {primaryKey} was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }


            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0", false);
            sql.Where($"{this.PrimaryKey} < @0", primaryKey);
            sql.Append($"ORDER BY {this.PrimaryKey} DESC");
            sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), 0);
            sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), 1);

            return (await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<dynamic> GetNextAsync(object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the get the next entity of \"{this.FullyQualifiedObjectName}\" by \"{this.PrimaryKey}\" with value {primaryKey} was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            //$"SELECT * FROM {this.FullyQualifiedObjectName} WHERE {this.PrimaryKey} > @0 
            //ORDER BY {this.PrimaryKey} LIMIT 1;";

            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0", false);
            sql.Where($"{this.PrimaryKey} > @0", primaryKey);
            sql.OrderBy(this.PrimaryKey);
            sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), 0);
            sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), 1);

            return (await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<dynamic> GetLastAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the get the last record of entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            //$"SELECT * FROM {this.FullyQualifiedObjectName} 
            //ORDER BY {this.PrimaryKey} DESC LIMIT 1;";

            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0", false);
            sql.Append($"ORDER BY {this.PrimaryKey} DESC");
            sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), 0);
            sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), 1);

            return (await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<IEnumerable<dynamic>> GetAsync(object[] primaryKeys)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. Keys: {primaryKeys}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var sql = new Sql("SELECT * FROM {this.FullyQualifiedObjectName}");
            sql.Where("deleted=@0", false);
            sql.Append("AND");
            sql.In("\"{this.PrimaryKey}\" IN (@0)", primaryKeys);


            return await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<CustomField>> GetCustomFieldsAsync(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to get custom fields for entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql;
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                sql =
                    $"SELECT * FROM config.custom_field_definition_view WHERE table_name='{this.FullyQualifiedObjectName}' ORDER BY field_order;";
                return await Factory.GetAsync<CustomField>(this.Database, sql).ConfigureAwait(false);
            }

            sql = FrapidDbServer.GetProcedureCommand
                (
                    this.Database,
                    "config.get_custom_field_definition",
                    new[]
                    {
                        "@0",
                        "@1"
                    });
            return
                await
                    Factory.GetAsync<CustomField>(this.Database, sql, this.FullyQualifiedObjectName, resourceId)
                        .ConfigureAwait(false);
        }

        public async Task<IEnumerable<DisplayField>> GetDisplayFieldsAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return new List<DisplayField>();
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to get display field for entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}",
                        this.LoginId);
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql =
                $"SELECT {this.PrimaryKey} AS \"key\", {this.NameColumn} as \"value\" FROM {this.FullyQualifiedObjectName} WHERE deleted=@0;";
            return await Factory.GetAsync<DisplayField>(this.Database, sql, false).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DisplayField>> GetLookupFieldsAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return new List<DisplayField>();
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to get display field for entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}",
                        this.LoginId);
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql =
                $"SELECT {this.LookupField} AS \"key\", {this.NameColumn} as \"value\" FROM {this.FullyQualifiedObjectName} WHERE deleted=@0;";
            return await Factory.GetAsync<DisplayField>(this.Database, sql, false).ConfigureAwait(false);
        }

        public async Task<object> AddOrEditAsync(Dictionary<string, object> item, List<CustomField> customFields)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            var primaryKeyValue = item.FirstOrDefault(x => x.Key.ToPascalCase().Equals(this.PrimaryKey.ToPascalCase())).Value;

            if (primaryKeyValue != null)
            {
                await this.UpdateAsync(item, primaryKeyValue, customFields).ConfigureAwait(false);
            }
            else
            {
                primaryKeyValue = await this.AddAsync(item, customFields, true).ConfigureAwait(false);
            }

            return primaryKeyValue;
        }

        public async Task<List<object>> BulkImportAsync(List<Dictionary<string, object>> items)
        {
            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.ImportData, this.LoginId, this.Database, false).ConfigureAwait(false);
                }

                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to import entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var result = new List<object>();
            int line = 0;
            using (var db = DbProvider.GetDatabase(this.Database))
            {
                try
                {
                    await db.BeginTransactionAsync().ConfigureAwait(false);

                    items = this.Crypt(items);

                    foreach (var item in items)
                    {
                        line++;

                        item["audit_user_id"] = this.UserId;
                        item["audit_ts"] = DateTimeOffset.UtcNow;
                        item["deleted"] = false;

                        var primaryKeyValue = item[this.PrimaryKey];

                        if (primaryKeyValue != null)
                        {
                            result.Add(primaryKeyValue);
                            var sql = new Sql("UPDATE " + this.FullyQualifiedObjectName + " SET");

                            int index = 0;

                            foreach (var prop in item.Where(x => !x.Key.Equals(this.PrimaryKey)))
                            {
                                if (index > 0)
                                {
                                    sql.Append(",");
                                }

                                sql.Append(Sanitizer.SanitizeIdentifierName(prop.Key) + "=@0", prop.Value);
                                index++;
                            }


                            sql.Where(this.PrimaryKey + "=@0", primaryKeyValue);

                            await db.NonQueryAsync(sql).ConfigureAwait(false);
                        }
                        else
                        {
                            string columns = string.Join(",",
                                item.Where(x => !x.Key.Equals(this.PrimaryKey))
                                    .Select(x => Sanitizer.SanitizeIdentifierName(x.Key)));

                            string parameters = string.Join(",",
                                Enumerable.Range(0, item.Count - 1).Select(x => "@" + x));
                            var arguments =
                                item.Where(x => !x.Key.Equals(this.PrimaryKey)).Select(x => x.Value).ToArray();

                            var sql = new Sql("INSERT INTO " + this.FullyQualifiedObjectName + "(" + columns + ")");
                            sql.Append("SELECT " + parameters, arguments);

                            sql.Append(FrapidDbServer.AddReturnInsertedKey(this.Database, this.PrimaryKey));

                            result.Add(await db.ScalarAsync<object>(sql).ConfigureAwait(false));
                        }
                    }

                    db.CommitTransaction();

                    return result;
                }
                catch (Exception ex)
                {
                    db.RollbackTransaction();
                    string errorMessage = $"Error on line {line}. {ex.Message} ";
                    throw new DataAccessException(errorMessage, ex);
                }
            }
        }

        public async Task UpdateAsync(Dictionary<string, object> item, object primaryKeyValue,
            List<CustomField> customFields)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Edit, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to edit entity \"{this.FullyQualifiedObjectName}\" with Primary Key {this.PrimaryKey} was denied to the user with Login ID {this.LoginId}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            item = this.Crypt(item);

            item["audit_user_id"] = this.UserId;
            item["audit_ts"] = DateTimeOffset.UtcNow;
            item["deleted"] = false;

            using (var db = DbProvider.GetDatabase(this.Database))
            {
                var sql = new Sql("UPDATE " + this.FullyQualifiedObjectName + " SET");

                int index = 0;

                foreach (var prop in item.Where(x => !x.Key.Equals(this.PrimaryKey)))
                {
                    if (index > 0)
                    {
                        sql.Append(",");
                    }

                    sql.Append(Sanitizer.SanitizeIdentifierName(prop.Key) + "=@0", prop.Value);
                    index++;
                }


                sql.Where(this.PrimaryKey + "=@0", primaryKeyValue);

                await db.NonQueryAsync(sql).ConfigureAwait(false);
                await this.AddCustomFieldsAsync(primaryKeyValue, customFields).ConfigureAwait(false);
            }
        }


        public async Task DeleteAsync(object primaryKey)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Delete, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to delete entity \"{this.FullyQualifiedObjectName}\" with Primary Key {this.PrimaryKey} was denied to the user with Login ID {this.LoginId}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            string sql = $"UPDATE {this.FullyQualifiedObjectName} SET deleted = @0, audit_user_id=@1, audit_ts=@2 WHERE {this.PrimaryKey}=@3;";
            await Factory.NonQueryAsync(this.Database, sql, true, this.UserId, DateTimeOffset.UtcNow, primaryKey).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetPaginatedResultAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to the first page of the entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }


            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0", false);
            sql.OrderBy(this.PrimaryKey);
            sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), 0);
            sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), PageSize);

            return await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetPaginatedResultAsync(long pageNumber)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to Page #{pageNumber} of the entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            long offset = (pageNumber - 1)*PageSize;
            string sql = $"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted=@0 ORDER BY {this.PrimaryKey}";

            sql += FrapidDbServer.AddOffset(this.Database, "@1");
            sql += FrapidDbServer.AddLimit(this.Database, PageSize.ToString());

            return await Factory.GetAsync<dynamic>(this.Database, sql, false, offset).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Filter>> GetFiltersAsync(string tenant, string filterName)
        {
            using (var db = DbProvider.GetDatabase(this.Database))
            {
                var sql = new Sql("SELECT * FROM config.filters");
                sql.Where("object_name = @0", this.FullyQualifiedObjectName);
                sql.And("LOWER(filter_name)=@0", filterName.ToLower());

                return await db.SelectAsync<Filter>(sql).ConfigureAwait(false);
            }
        }

        public async Task<long> CountWhereAsync(List<Filter> filters)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return 0;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to count entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. Filters: {filters}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var sql = new Sql($"SELECT COUNT(*) FROM {this.FullyQualifiedObjectName} WHERE deleted = @0", false);
            FilterManager.AddFilters(ref sql, filters);

            return await Factory.ScalarAsync<long>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetWhereAsync(long pageNumber, List<Filter> filters)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to Page #{pageNumber} of the filtered entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. Filters: {filters}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            long offset = (pageNumber - 1)*PageSize;
            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted = @0", false);

            FilterManager.AddFilters(ref sql, filters);

            if (!string.IsNullOrWhiteSpace(this.PrimaryKey))
            {
                sql.OrderBy(this.PrimaryKey);
            }

            if (pageNumber > 0)
            {
                sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), offset);
                sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), PageSize);
            }

            return await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<long> CountFilteredAsync(string filterName)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return 0;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to count entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. Filter: {filterName}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var filters = await this.GetFiltersAsync(this.Database, filterName).ConfigureAwait(false);
            var sql = new Sql($"SELECT COUNT(*) FROM {this.FullyQualifiedObjectName} WHERE deleted = @0", false);
            FilterManager.AddFilters(ref sql, filters.ToList());

            return await Factory.ScalarAsync<long>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> GetFilteredAsync(long pageNumber, string filterName)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to Page #{pageNumber} of the filtered entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. Filter: {filterName}.");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var filters = await this.GetFiltersAsync(this.Database, filterName).ConfigureAwait(false);

            long offset = (pageNumber - 1)*PageSize;
            var sql = new Sql($"SELECT * FROM {this.FullyQualifiedObjectName} WHERE deleted = @0", false);

            FilterManager.AddFilters(ref sql, filters.ToList());

            if (!string.IsNullOrWhiteSpace(this.PrimaryKey))
            {
                sql.OrderBy(this.PrimaryKey);
            }

            if (pageNumber > 0)
            {
                sql.Append(FrapidDbServer.AddOffset(this.Database, "@0"), offset);
                sql.Append(FrapidDbServer.AddLimit(this.Database, "@0"), PageSize);
            }

            return await Factory.GetAsync<dynamic>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<object> AddAsync(Dictionary<string, object> item, List<CustomField> customFields,
            bool skipPrimaryKey)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Create, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to add entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}. {item}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            item = this.Crypt(item);

            item["audit_user_id"] = this.UserId;
            item["audit_ts"] = DateTimeOffset.UtcNow;
            item["deleted"] = false;

            using (var db = DbProvider.GetDatabase(this.Database))
            {
                string columns = string.Join
                    (",",
                        skipPrimaryKey
                            ? item.Where(x => !x.Key.ToUnderscoreLowerCase().Equals(this.PrimaryKey))
                                .Select(x => Sanitizer.SanitizeIdentifierName(x.Key).ToUnderscoreLowerCase())
                            : item.Select(x => Sanitizer.SanitizeIdentifierName(x.Key).ToUnderscoreLowerCase()));

                string parameters = string.Join(",",
                    Enumerable.Range(0, skipPrimaryKey ? item.Count - 1 : item.Count).Select(x => "@" + x));

                var arguments = skipPrimaryKey
                    ? item.Where(x => !x.Key.ToUnderscoreLowerCase().Equals(this.PrimaryKey))
                        .Select(x => x.Value).ToArray()
                    : item.Select(x => x.Value).ToArray();

                var sql = new Sql("INSERT INTO " + this.FullyQualifiedObjectName + "(" + columns + ")");
                sql.Append("SELECT " + parameters, arguments);

                sql.Append(FrapidDbServer.AddReturnInsertedKey(this.Database, this.PrimaryKey));

                var primaryKeyValue = await db.ScalarAsync<object>(sql).ConfigureAwait(false);
                await this.AddCustomFieldsAsync(primaryKeyValue, customFields).ConfigureAwait(false);
                return primaryKeyValue;
            }
        }

        public async Task<IEnumerable<DisplayField>> GetDisplayFieldsAsync(List<Filter> filters)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return new List<DisplayField>();
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to get display field for entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}",
                        this.LoginId);
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var sql = new Sql($"SELECT {this.PrimaryKey} AS \"key\", {this.NameColumn} as \"value\" FROM {this.FullyQualifiedObjectName} WHERE deleted=@0 ", false);

            FilterManager.AddFilters(ref sql, filters);
            sql.OrderBy("1");

            return await Factory.GetAsync<DisplayField>(this.Database, sql).ConfigureAwait(false);
        }

        public async Task<IEnumerable<DisplayField>> GetLookupFieldsAsync(List<Filter> filters)
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return new List<DisplayField>();
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to get display field for entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}",
                        this.LoginId);
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            var sql = new Sql($"SELECT {this.LookupField} AS \"key\", {this.NameColumn} as \"value\" FROM {this.FullyQualifiedObjectName} WHERE deleted=@0 ", false);

            FilterManager.AddFilters(ref sql, filters);
            sql.OrderBy("1");

            return await Factory.GetAsync<DisplayField>(this.Database, sql).ConfigureAwait(false);
        }

        private List<Dictionary<string, object>> Crypt(List<Dictionary<string, object>> items)
        {
            for (int index = 0; index < items.Count; index++)
            {
                items[index] = this.Crypt(items[index]);
            }

            return items;
        }

        private Dictionary<string, object> Crypt(Dictionary<string, object> item)
        {
            for (int index = 0; index < item.Count; index++)
            {
                var candidate = item.ElementAt(index);

                if (candidate.Key.ToUpperInvariant().Contains("PASSWORD") ||
                    candidate.Key.ToUpperInvariant().Contains("SECRET"))
                {
                    var bytes = Encoding.UTF8.GetBytes(candidate.Value.ToString());
                    item[candidate.Key] = Encoding.UTF8.GetString(MachineKey.Protect(bytes, "ScrudFactory"));
                }
            }

            return item;
        }

        public async Task AddCustomFieldsAsync(object primaryKeyValue, List<CustomField> customFields)
        {
            string sql = $"DELETE FROM config.custom_fields WHERE custom_field_setup_id IN(" +
                         "SELECT custom_field_setup_id " + "FROM config.custom_field_setup " +
                         "WHERE form_name=config.get_custom_field_form_name('{this.FullyQualifiedObjectName}')" + ");";

            await Factory.NonQueryAsync(this.Database, sql).ConfigureAwait(false);

            if (customFields == null)
            {
                return;
            }

            foreach (var field in customFields)
            {
                sql = $"INSERT INTO config.custom_fields(custom_field_setup_id, resource_id, value) " +
                      "SELECT config.get_custom_field_setup_id_by_table_name('{this.FullyQualifiedObjectName}', @0), " +
                      "@1, @2;";

                await
                    Factory.NonQueryAsync(this.Database, sql, field.FieldName, primaryKeyValue, field.Value)
                        .ConfigureAwait(false);
            }
        }

        private string GetTableName()
        {
            string tableName = this._ObjectName.Replace("-", "_");
            return tableName;
        }

        private string GetCandidateKey()
        {
            string candidateKey = Inflector.MakeSingular(this._ObjectName);
            if (!string.IsNullOrWhiteSpace(candidateKey))
            {
                candidateKey += "_id";
            }

            candidateKey = candidateKey ?? "";

            return Sanitizer.SanitizeIdentifierName(candidateKey);
        }

        private string GetLookupField()
        {
            string candidateKey = Inflector.MakeSingular(this._ObjectName);
            if (!string.IsNullOrWhiteSpace(candidateKey))
            {
                candidateKey += "_code";
            }

            candidateKey = candidateKey ?? "";

            return Sanitizer.SanitizeIdentifierName(candidateKey);
        }

        private string GetNameColumn()
        {
            string nameKey = Inflector.MakeSingular(this._ObjectName);

            if (!string.IsNullOrWhiteSpace(nameKey))
            {
                nameKey += "_name";
            }

            return nameKey ?? "";
        }

        public async Task<EntityView> GetMetaAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Database))
            {
                return null;
            }

            if (!this.SkipValidation)
            {
                if (!this.Validated)
                {
                    await this.ValidateAsync(AccessTypeEnum.Read, this.LoginId, this.Database, false).ConfigureAwait(false);
                }
                if (!this.HasAccess)
                {
                    Log.Information(
                        $"Access to view meta information on entity \"{this.FullyQualifiedObjectName}\" was denied to the user with Login ID {this.LoginId}");
                    throw new UnauthorizedException("Access is denied.");
                }
            }

            return
                await
                    EntityView.GetAsync(this.Database, this.PrimaryKey, this._ObjectNamespace, this.GetTableName())
                        .ConfigureAwait(false);
        }
    }
}