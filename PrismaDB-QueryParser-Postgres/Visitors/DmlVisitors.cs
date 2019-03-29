﻿using Antlr4.Runtime.Misc;
using PrismaDB.Commons;
using PrismaDB.QueryAST;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryParser.Postgres.AntlrGrammer;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.Postgres
{
    public partial class PostgresVisitor : PostgresParserBaseVisitor<object>
    {
        public override object VisitInsertStatement([NotNull] PostgresParser.InsertStatementContext context)
        {
            var res = new InsertQuery();
            res.Into = (TableRef)Visit(context.tableName());
            if (context.uidList() != null)
                foreach (var id in (List<Identifier>)Visit(context.uidList()))
                    res.Columns.Add(new ColumnRef(id));
            res.Values = (List<List<Expression>>)Visit(context.insertStatementValue());
            return res;
        }

        public override object VisitSelectStatement([NotNull] PostgresParser.SelectStatementContext context)
        {
            var res = new SelectQuery();
            res.SelectExpressions = (List<Expression>)Visit(context.selectElements());
            if (context.fromClause() != null)
            {
                var from = (SelectQuery)Visit(context.fromClause());
                res.FromTables = from.FromTables;
                res.FromSubQueries = from.FromSubQueries;
                res.Joins = from.Joins;
            }
            if (context.whereClause() != null)
                res.Where = (WhereClause)Visit(context.whereClause());
            if (context.groupByClause() != null)
                res.GroupBy = (GroupByClause)Visit(context.groupByClause());
            if (context.orderByClause() != null)
                res.OrderBy = (OrderByClause)Visit(context.orderByClause());
            if (context.limitClause() != null)
                res.Limit = (uint?)Visit(context.limitClause());
            return res;
        }

        public override object VisitInsertStatementValue([NotNull] PostgresParser.InsertStatementValueContext context)
        {
            var res = new List<List<Expression>>();
            foreach (var exps in context.expressions())
                res.Add((List<Expression>)Visit(exps));
            return res;
        }

        public override object VisitUpdatedElement([NotNull] PostgresParser.UpdatedElementContext context)
        {
            var res = new Pair<ColumnRef, Constant>();
            res.First = (ColumnRef)Visit(context.fullColumnName());
            res.Second = (Constant)Visit(context.expression());
            return res;
        }

        public override object VisitSingleDeleteStatement([NotNull] PostgresParser.SingleDeleteStatementContext context)
        {
            var res = new DeleteQuery();
            res.DeleteTable = (TableRef)Visit(context.tableName());
            if (context.expression() != null)
                res.Where = ExpressionToCnfWhere(context.expression());
            return res;
        }

        public override object VisitSingleUpdateStatement([NotNull] PostgresParser.SingleUpdateStatementContext context)
        {
            var res = new UpdateQuery();
            res.UpdateTable = (TableRef)Visit(context.tableName());
            foreach (var updatedElement in context.updatedElement())
                res.UpdateExpressions.Add((Pair<ColumnRef, Constant>)Visit(updatedElement));
            if (context.expression() != null)
                res.Where = ExpressionToCnfWhere(context.expression());
            return res;
        }

        public override object VisitOrderByClause([NotNull] PostgresParser.OrderByClauseContext context)
        {
            var res = new OrderByClause();
            foreach (var orderByExp in context.orderByExpression())
                res.OrderColumns.Add((Pair<ColumnRef, OrderDirection>)Visit(orderByExp));
            return res;
        }

        public override object VisitOrderByExpression([NotNull] PostgresParser.OrderByExpressionContext context)
        {
            var res = new Pair<ColumnRef, OrderDirection>();
            res.First = (ColumnRef)Visit(context.expression());
            res.Second = OrderDirection.ASC;
            if (context.DESC() != null)
                res.Second = OrderDirection.DESC;
            return res;
        }

        public override object VisitTableSources([NotNull] PostgresParser.TableSourcesContext context)
        {
            var res = new List<object>();
            foreach (var tableSource in context.tableSourceItem())
                res.Add(Visit(tableSource));
            return res;
        }

        public override object VisitTableSourceItem([NotNull] PostgresParser.TableSourceItemContext context)
        {
            if (context.tableName() != null)
            {
                var res = (TableRef)Visit(context.tableName());
                if (context.alias != null)
                    res.Alias = (Identifier)Visit(context.uid());
                return res;
            }
            else if (context.selectStatement() != null)
            {
                var res = new SelectSubQuery();
                res.Select = (SelectQuery)Visit(context.selectStatement());
                if (context.alias != null)
                    res.Alias = (Identifier)Visit(context.uid());
                return res;
            }
            return null;
        }

        public override object VisitInnerJoin([NotNull] PostgresParser.InnerJoinContext context)
        {
            var res = new JoinClause();
            res.JoinType = JoinType.INNER;
            if (context.CROSS() != null)
                res.JoinType = JoinType.CROSS;
            res.JoinTable = (TableRef)Visit(context.tableSourceItem());
            if (context.ON() != null)
            {
                var exp = (BooleanEquals)Visit(context.expression());
                res.FirstColumn = (ColumnRef)exp.left;
                res.SecondColumn = (ColumnRef)exp.right;
            }
            return res;
        }

        public override object VisitOuterJoin([NotNull] PostgresParser.OuterJoinContext context)
        {
            var res = new JoinClause();
            if (context.LEFT() != null)
                res.JoinType = JoinType.LEFT_OUTER;
            else if (context.RIGHT() != null)
                res.JoinType = JoinType.RIGHT_OUTER;
            res.JoinTable = (TableRef)Visit(context.tableSourceItem());
            if (context.ON() != null)
            {
                var exp = (BooleanEquals)Visit(context.expression());
                res.FirstColumn = (ColumnRef)exp.left;
                res.SecondColumn = (ColumnRef)exp.right;
            }
            return res;
        }

        public override object VisitSelectElements([NotNull] PostgresParser.SelectElementsContext context)
        {
            var res = new List<Expression>();

            if (context.star != null)
                res.Add(new AllColumns());

            foreach (var element in context.selectElement())
                res.Add((Expression)Visit(element));

            return res;
        }

        public override object VisitSelectStarElement([NotNull] PostgresParser.SelectStarElementContext context)
        {
            return new AllColumns(((Identifier)Visit(context.uid())).id);
        }

        public override object VisitSelectColumnElement([NotNull] PostgresParser.SelectColumnElementContext context)
        {
            var res = (ColumnRef)Visit(context.fullColumnName());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            return res;
        }

        public override object VisitSelectFunctionElement([NotNull] PostgresParser.SelectFunctionElementContext context)
        {
            var res = (ScalarFunction)Visit(context.functionCall());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.functionCall().GetText());
            return res;
        }

        public override object VisitSelectExpressionElement([NotNull] PostgresParser.SelectExpressionElementContext context)
        {
            var res = (Expression)Visit(context.expression());
            if (context.uid() != null)
                res.Alias = (Identifier)Visit(context.uid());
            else
                res.Alias = new Identifier(context.expression().GetText());
            return res;
        }

        public override object VisitFromClause([NotNull] PostgresParser.FromClauseContext context)
        {
            var res = new SelectQuery();
            foreach (var source in (List<object>)Visit(context.tableSources()))
            {
                if (source is TableRef tableRef)
                    res.FromTables.Add(tableRef);
                else if (source is SelectSubQuery selectSubQuery)
                    res.FromSubQueries.Add(selectSubQuery);
            }
            res.Joins = new List<JoinClause>();
            foreach (var joinPart in context.joinPart())
                res.Joins.Add((JoinClause)Visit(joinPart));
            return res;
        }

        public override object VisitWhereClause([NotNull] PostgresParser.WhereClauseContext context)
        {
            return ExpressionToCnfWhere(context.whereExpr);
        }

        public override object VisitGroupByClause([NotNull] PostgresParser.GroupByClauseContext context)
        {
            var res = new GroupByClause();
            foreach (var groupByItem in context.groupByItem())
                res.GroupColumns.Add((ColumnRef)Visit(groupByItem));
            return res;
        }

        public override object VisitGroupByItem([NotNull] PostgresParser.GroupByItemContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitLimitClause([NotNull] PostgresParser.LimitClauseContext context)
        {
            return (uint?)((IntConstant)Visit(context.intLiteral())).intvalue;
        }

        public WhereClause ExpressionToCnfWhere(PostgresParser.ExpressionContext context)
        {
            var res = new WhereClause();
            var expr = (Expression)Visit(context);
            while (!CnfConverter.CheckCnf(expr)) expr = CnfConverter.ConvertToCnf(expr);
            res.CNF = CnfConverter.BuildCnf(expr);
            return res;
        }
    }
}
