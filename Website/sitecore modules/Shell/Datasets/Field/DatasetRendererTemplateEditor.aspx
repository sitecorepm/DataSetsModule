<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DatasetRendererTemplateEditor.aspx.cs"
    Inherits="Sitecore.SharedSource.Dataset.UI.DatasetRenderer.DatasetRendererTemplateEditor" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Template Editor</title>
    <base target="_self" />
    <style type="text/css">
        html, body
        {
            padding: 0px;
            background-color: rgb(240, 241, 242);
        }
        textarea
        {
            padding: 0px;
            margin: 0px;
        }
        .header
        {
            font-family: Tahoma;
            font-size: 8pt;
            font-weight: bold;
        }
        .field
        {
            white-space: nowrap; /*cursor: pointer;*/
            float: left;
            font: 10pt Verdana;
            border: solid 1px #CAD8F3;
            background-color: #DEE7F8;
            border: solid 1px #999999;
            padding: 0px 2px 0px 2px;
            margin: 1px 2px 1px 2px;
            width: auto;
        }
        .field-container
        {
            border: 1px solid #cccccc;
            margin: 3px 0px 5px 0px;
            padding: 2px;
            height: 100px;
            overflow-x: hidden;
            overflow-y: auto;
        }
        .editor-content
        {
            width: 970px;
            padding: 0px 10px;
        }
    </style>
    <link rel="stylesheet" type="text/css" href="../styles/smoothness/jquery-ui-1.8.18.custom.css" />
    <script src="/sitecore/shell/Controls/InternetExplorer.js" type="text/javascript"></script>
    <script src="/sitecore/shell/Controls/Sitecore.js" type="text/javascript"></script>
    <script src="../js/jquery-1.7.2.min.js" type="text/javascript"></script>
    <script src="../js/jquery-ui-1.8.16.custom.min.js" type="text/javascript"></script>
    <script src="../js/jquery.insertAtCaret.js" type="text/javascript"></script>
    <script type="text/javascript">
        var _$TextFunctionDialog = {};
        $j(function () {
            $j('.field').click(function () {
                $j('#<%= txtTemplate.ClientID%>').insertAtCaret($j(this).text());
            });
            $j('div.text-function-descriptions').accordion();
            //            _$TextFunctionDialog = $j('#text-function-dialog-content').dialog({ autoOpen: false, title: 'Help', width: 900 });
            //            $j('#btnTextFunctionHelp').click(function () { _$TextFunctionDialog.dialog('open'); });
        });
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:HiddenField ID="hdnDatasetRendererID" runat="server" />
    <asp:HiddenField ID="hdnContentDBName" runat="server" />
    <div class="editor-content">
        <span class="header">Available Fields: click to insert below...</span>
        <div class="field-container">
            <div>
                <asp:Repeater ID="rptrFields" runat="server">
                    <ItemTemplate>
                        <span class="field" ondblclick="window.clipboardData.setData('Text',this.innerText);">[<%# Container.DataItem %>]</span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
        <div style="margin: 5px 0px;">
            <asp:TextBox ID="txtTemplate" runat="server" TextMode="MultiLine" Rows="20" Width="100%"></asp:TextBox>
        </div>
        <div>
            <div style="float: right">
                <asp:Button ID="btnOK" Width="100px" runat="server" Text="OK" OnClick="btnOK_Click" />
                <asp:Button ID="btnCancel" Width="100px" runat="server" Text="Cancel" OnClick="btnCancel_Click" />
            </div>
        </div>
    </div>
    </form>
</body>
</html>
