﻿using Antlr4.Runtime.Misc;
using PrismaDB.QueryAST.DCL;
using PrismaDB.QueryAST.DDL;
using PrismaDB.QueryAST.DML;
using PrismaDB.QueryParser.Postgres.AntlrGrammer;
using System.Collections.Generic;

namespace PrismaDB.QueryParser.Postgres
{
    public partial class PostgresVisitor : PostgresParserBaseVisitor<object>
    {
        public override object VisitExportSettingsCommand([NotNull] PostgresParser.ExportSettingsCommandContext context)
        {
            return new ExportSettingsCommand(((StringConstant)Visit(context.stringLiteral())).strvalue);
        }

        public override object VisitUpdateKeysCommand([NotNull] PostgresParser.UpdateKeysCommandContext context)
        {
            var res = new UpdateKeysCommand();
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitEncryptCommand([NotNull] PostgresParser.EncryptCommandContext context)
        {
            var res = new EncryptColumnCommand();
            res.Column = (ColumnRef)Visit(context.fullColumnName());
            res.EncryptionFlags = ColumnEncryptionFlags.Store;
            if (context.encryptionOptions() != null)
                res.EncryptionFlags = (ColumnEncryptionFlags)Visit(context.encryptionOptions());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitDecryptCommand([NotNull] PostgresParser.DecryptCommandContext context)
        {
            var res = new DecryptColumnCommand();
            res.Column = (ColumnRef)Visit(context.fullColumnName());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }

        public override object VisitRebalanceOpetreeCommand([NotNull] PostgresParser.RebalanceOpetreeCommandContext context)
        {
            var res = new RebalanceOpetreeCommand();
            if (context.constants() != null)
                res.WithValues = (List<Constant>)Visit(context.constants());
            if (context.STATUS() != null)
                res.StatusCheck = true;
            return res;
        }
    }
}