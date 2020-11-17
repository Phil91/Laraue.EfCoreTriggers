﻿using Laraue.EfCoreTriggers.Common.Builders.Triggers.Base;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Builders.Providers
{
    public class SqlLiteProvider : BaseTriggerProvider
    {
        public SqlLiteProvider(IModel model) : base(model)
        {
        }

        protected override string NewEntityPrefix => "NEW";

        protected override string OldEntityPrefix => "OLD";

        public override GeneratedSql GetDropTriggerSql(string triggerName, Type entityType)
            => new GeneratedSql().Append($"DROP TRIGGER {triggerName}");

        public override GeneratedSql GetTriggerActionsSql<TTriggerEntity>(TriggerActions<TTriggerEntity> triggerActions)
        {
            var sqlResult = new GeneratedSql();

            /*if (triggerActions.ActionConditions.Count > 0)
            {
                var conditionsSql = triggerActions.ActionConditions.Select(actionCondition => actionCondition.BuildSql(this));
                sqlResult.MergeColumnsInfo(conditionsSql);
                sqlResult.Append($"WHEN ")
                    .AppendJoin(" AND ", conditionsSql.Select(x => x.SqlBuilder))
                    .Append($" THEN ");
            }*/

            var actionsSql = triggerActions.ActionExpressions.Select(action => action.BuildSql(this));
            sqlResult.MergeColumnsInfo(actionsSql)
                .AppendJoin(", ", actionsSql.Select(x => x.SqlBuilder));

            /*if (triggerActions.ActionConditions.Count > 0)
            {
                sqlResult
                    .Append($" END; ");
            }*/

            return sqlResult;
        }

        protected override string GetExpressionTypeSql(ExpressionType expressionType) => expressionType switch
        {
            ExpressionType.IsTrue => "= 1",
            ExpressionType.IsFalse => "= 0",
            ExpressionType.Not => "= 0",
            _ => base.GetExpressionTypeSql(expressionType),
        };

        public override GeneratedSql GetTriggerSql<TTriggerEntity>(Trigger<TTriggerEntity> trigger)
        {
            var triggerTypes = new Dictionary<TriggerType, string>
            {
                [TriggerType.After] = "AFTER",
                [TriggerType.Before] = "BEFORE",
                [TriggerType.InsteadOf] = "INSTEAD OF",
            };

            if (!triggerTypes.TryGetValue(trigger.TriggerType, out var triggerTypeName))
                throw new NotSupportedException($"Trigger type {trigger.TriggerType} is not supported for {nameof(SqlLiteProvider)}.");

            var actionsSql = trigger.Actions.Select(action => action.BuildSql(this));
            var generatedSql = new GeneratedSql(actionsSql)
                .Append($"CREATE TRIGGER {trigger.Name} {triggerTypeName} {trigger.TriggerAction.ToString().ToUpper()} ")
                .Append($"ON {GetTableName(typeof(TTriggerEntity))} FOR EACH ROW BEGIN ")
                .AppendJoin(actionsSql.Select(x => x.SqlBuilder))
                .Append(" END");

            return generatedSql;
        }

        public override GeneratedSql GetTriggerUpsertActionSql<TTriggerEntity, TUpdateEntity>(TriggerUpsertAction<TTriggerEntity, TUpdateEntity> triggerUpsertAction)
        {
            var insertStatementSql = GetInsertStatementBodySql(triggerUpsertAction.InsertExpression, triggerUpsertAction.InsertExpressionPrefixes);
            var newExpressionColumnsSql = GetNewExpressionColumnsSql(
                (NewExpression)triggerUpsertAction.MatchExpression.Body,
                triggerUpsertAction.MatchExpressionPrefixes.ToDictionary(x => x.Key, x => ArgumentType.None));

            var sqlBuilder = new GeneratedSql(insertStatementSql.AffectedColumns)
                .MergeColumnsInfo(newExpressionColumnsSql)
                .Append($"INSERT INTO {GetTableName(typeof(TUpdateEntity))} ")
                .Append(insertStatementSql.SqlBuilder)
                .Append($" ON CONFLICT (")
                .AppendJoin(", ", newExpressionColumnsSql.Select(x => x.SqlBuilder))
                .Append(")");

            if (triggerUpsertAction.OnMatchExpression is null)
            {
                sqlBuilder.Append(" DO NOTHING;");
            }
            else
            {
                var updateStatementSql = GetUpdateStatementBodySql(triggerUpsertAction.OnMatchExpression, triggerUpsertAction.OnMatchExpressionPrefixes);
                sqlBuilder.MergeColumnsInfo(updateStatementSql.AffectedColumns)
                    .Append($" DO UPDATE SET ")
                    .Append(updateStatementSql.SqlBuilder)
                    .Append(";");
            }

            return sqlBuilder;
        }
    }
}
