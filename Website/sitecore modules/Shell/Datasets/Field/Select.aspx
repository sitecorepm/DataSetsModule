<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Select.aspx.cs" Inherits="Sitecore.SharedSource.Dataset.UI.DatasetRenderer.Select" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script type="text/javascript">
        function CheckSelectedFields() {
            var value = $j.trim($j('#<%= hdnValue.ClientID %>').attr('value'));
            if (value.length == 0)
                SelectAll();
            else {
                $j('#select-fields input').each(function () {
                    if (value.indexOf($j(this).attr('id')) != -1) {
                        $j(this).attr('checked', 'on');
                    }
                });
            }
        }
        function SelectAll() {
            $j('#select-fields input').attr('checked', 'on');
        }
        function ClearAll() {
            $j('#select-fields input').attr('checked', '');
        }
        function GetValue() {
            var names = [];
            $j('#select-fields input:checked').each(function () {
                names.push($j(this).attr('id'));
            });
            return names.join('|');
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div style="padding: 2px 2px 2px 2px;">
        <asp:HiddenField ID="hdnValue" runat="server"></asp:HiddenField>
        <asp:Panel ID="pnlDatasetComponentSource" runat="server" Visible="false">
            <asp:TextBox ID="txtValue" runat="server" TextMode="MultiLine" Rows="3" Columns="100"></asp:TextBox>
        </asp:Panel>
        <asp:Panel ID="pnlDatasetRenderer" runat="server">
            <input type="button" value="All" onclick="SelectAll();" />
            <input type="button" value="Clear" onclick="ClearAll();" />
            <%--<asp:CheckBoxList ID="cbxList" runat="server" RepeatDirection="Horizontal" RepeatColumns="5">
            </asp:CheckBoxList>--%>
            <asp:Repeater ID="rptrList" runat="server">
                <HeaderTemplate>
                    <table id='select-fields'>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <%# Container.DataItem %>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </asp:Panel>
    </div>
    </form>
</body>
</html>
