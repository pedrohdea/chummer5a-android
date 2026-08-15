/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

// SOMBRAS DE WINFORMS PARA A SONDAGEM EXECUTÁVEL — não faz parte de nenhum projeto do
// produto, e mora fora de Chummer/ e de src/ para não ser apanhado por build nenhum.
//
// POR QUE PRECISAM EXISTIR
//
// A sondagem de carga compila Chummer/Controls/Dominio/** junto com o Backend, porque as
// metades extraídas ainda contêm método de DOMÍNIO que o Backend chama
// (Character.ClearInitiations, os métodos de atributo de matriz). Stubá-los mudaria regra
// de negócio; compilá-los de verdade exige que os tipos de WinForms que aparecem nas
// assinaturas existam.
//
// A DIFERENÇA QUE IMPORTA, E QUE É O PONTO DO SPIKE
//
// Tudo aqui LANÇA ao ser usado. Se carregar um .chum5 tocar um TreeNode, a exceção diz
// isso, e o resultado do spike passa a ser "carregar toca a árvore de UI" — que é achado,
// não obstáculo. O que NÃO se faz aqui é implementar comportamento: uma sombra que
// funciona esconde exatamente o acoplamento que a sondagem existe para medir.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace System.Windows.Forms
{
    internal static class Sombra
    {
        internal static Exception Erro(string strMembro)
            => new NotSupportedException("UI de WinForms tocada em execução: " + strMembro);
    }

    public enum SortOrder
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }

    public enum RightToLeft
    {
        No = 0,
        Yes = 1,
        Inherit = 2
    }

    public enum Keys
    {
        None = 0,
        Delete = 46
    }

    public enum MessageBoxDefaultButton
    {
        Button1 = 0, Button2 = 256, Button3 = 512
    }

    public interface IWin32Window
    {
        IntPtr Handle { get; }
    }

    public class Component : IDisposable
    {
        public virtual void Dispose() { }
    }

    public class Control : Component, IWin32Window
    {
        public IntPtr Handle => throw Sombra.Erro("Control.Handle");
        public bool IsHandleCreated => throw Sombra.Erro("Control.IsHandleCreated");
        public bool IsDisposed => throw Sombra.Erro("Control.IsDisposed");
        public bool InvokeRequired => throw Sombra.Erro("Control.InvokeRequired");
        public bool Disposing => throw Sombra.Erro("Control.Disposing");
        public bool Visible { get => throw Sombra.Erro("Control.Visible"); set => throw Sombra.Erro("Control.Visible"); }
        public bool Enabled { get => throw Sombra.Erro("Control.Enabled"); set => throw Sombra.Erro("Control.Enabled"); }
        public string Text { get => throw Sombra.Erro("Control.Text"); set => throw Sombra.Erro("Control.Text"); }
        public string Name { get => throw Sombra.Erro("Control.Name"); set => throw Sombra.Erro("Control.Name"); }
        public object Tag { get => throw Sombra.Erro("Control.Tag"); set => throw Sombra.Erro("Control.Tag"); }
        public Color ForeColor { get => throw Sombra.Erro("Control.ForeColor"); set => throw Sombra.Erro("Control.ForeColor"); }
        public Color BackColor { get => throw Sombra.Erro("Control.BackColor"); set => throw Sombra.Erro("Control.BackColor"); }
        public Control Parent { get => throw Sombra.Erro("Control.Parent"); set => throw Sombra.Erro("Control.Parent"); }
        public RightToLeft RightToLeft { get => throw Sombra.Erro("Control.RightToLeft"); set => throw Sombra.Erro("Control.RightToLeft"); }
        public ControlCollection Controls => throw Sombra.Erro("Control.Controls");
        public ContextMenuStrip ContextMenuStrip { get => throw Sombra.Erro("Control.ContextMenuStrip"); set => throw Sombra.Erro("Control.ContextMenuStrip"); }
        public Font Font { get => throw Sombra.Erro("Control.Font"); set => throw Sombra.Erro("Control.Font"); }
        public BindingContext BindingContext { get => throw Sombra.Erro("Control.BindingContext"); set => throw Sombra.Erro("Control.BindingContext"); }
        public void SuspendLayout() => throw Sombra.Erro("Control.SuspendLayout");
        public void ResumeLayout(bool performLayout = true) => throw Sombra.Erro("Control.ResumeLayout");
        public void CreateControl() => throw Sombra.Erro("Control.CreateControl");
        public void Refresh() => throw Sombra.Erro("Control.Refresh");
        public Form FindForm() => throw Sombra.Erro("Control.FindForm");
        public object Invoke(Delegate method) => throw Sombra.Erro("Control.Invoke");
        public IAsyncResult BeginInvoke(Delegate method) => throw Sombra.Erro("Control.BeginInvoke");

        public class ControlCollection : IEnumerable<Control>
        {
            public int Count => throw Sombra.Erro("Control.Controls.Count");
            public Control this[int i] => throw Sombra.Erro("Control.Controls[]");
            public IEnumerator<Control> GetEnumerator() => throw Sombra.Erro("Control.Controls");
            IEnumerator IEnumerable.GetEnumerator() => throw Sombra.Erro("Control.Controls");
        }
    }

    public class Form : Control
    {
        public double Opacity { get => throw Sombra.Erro("Form.Opacity"); set => throw Sombra.Erro("Form.Opacity"); }
        public void Close() => throw Sombra.Erro("Form.Close");
    }

    public class ToolStripItem : Component
    {
        public string Text { get => throw Sombra.Erro("ToolStripItem.Text"); set => throw Sombra.Erro("ToolStripItem.Text"); }
        public object Tag { get => throw Sombra.Erro("ToolStripItem.Tag"); set => throw Sombra.Erro("ToolStripItem.Tag"); }
    }

    public class ToolStrip : Control
    {
        public IList<ToolStripItem> Items => throw Sombra.Erro("ToolStrip.Items");
    }

    public class ContextMenuStrip : ToolStrip
    {
    }

    public class ToolTip : Component
    {
        public void SetToolTip(Control objControl, string strCaption)
            => throw Sombra.Erro("ToolTip.SetToolTip");
    }

    public class RichTextBox : Control
    {
        public string Rtf { get => throw Sombra.Erro("RichTextBox.Rtf"); set => throw Sombra.Erro("RichTextBox.Rtf"); }
    }

    public class TreeNode : ICloneable
    {
        public string Text { get => throw Sombra.Erro("TreeNode.Text"); set => throw Sombra.Erro("TreeNode.Text"); }
        public string Name { get => throw Sombra.Erro("TreeNode.Name"); set => throw Sombra.Erro("TreeNode.Name"); }
        public string ToolTipText { get => throw Sombra.Erro("TreeNode.ToolTipText"); set => throw Sombra.Erro("TreeNode.ToolTipText"); }
        public object Tag { get => throw Sombra.Erro("TreeNode.Tag"); set => throw Sombra.Erro("TreeNode.Tag"); }
        public Color ForeColor { get => throw Sombra.Erro("TreeNode.ForeColor"); set => throw Sombra.Erro("TreeNode.ForeColor"); }
        public ContextMenuStrip ContextMenuStrip { get => throw Sombra.Erro("TreeNode.ContextMenuStrip"); set => throw Sombra.Erro("TreeNode.ContextMenuStrip"); }
        public bool Checked { get => throw Sombra.Erro("TreeNode.Checked"); set => throw Sombra.Erro("TreeNode.Checked"); }
        public Font NodeFont { get => throw Sombra.Erro("TreeNode.NodeFont"); set => throw Sombra.Erro("TreeNode.NodeFont"); }
        public int Level => throw Sombra.Erro("TreeNode.Level");
        public int Index => throw Sombra.Erro("TreeNode.Index");
        public TreeNode Parent => throw Sombra.Erro("TreeNode.Parent");
        public TreeView TreeView => throw Sombra.Erro("TreeNode.TreeView");
        public TreeNodeCollection Nodes => throw Sombra.Erro("TreeNode.Nodes");
        public void Expand() => throw Sombra.Erro("TreeNode.Expand");
        public void Remove() => throw Sombra.Erro("TreeNode.Remove");
        public object Clone() => throw Sombra.Erro("TreeNode.Clone");
    }

    public class TreeNodeCollection : IEnumerable<TreeNode>
    {
        public int Count => throw Sombra.Erro("TreeNodeCollection.Count");
        public TreeNode this[int i] => throw Sombra.Erro("TreeNodeCollection[]");
        public int Add(TreeNode node) => throw Sombra.Erro("TreeNodeCollection.Add");
        public void Insert(int index, TreeNode node) => throw Sombra.Erro("TreeNodeCollection.Insert");
        public void Remove(TreeNode node) => throw Sombra.Erro("TreeNodeCollection.Remove");
        public void Clear() => throw Sombra.Erro("TreeNodeCollection.Clear");
        public IEnumerator<TreeNode> GetEnumerator() => throw Sombra.Erro("TreeNodeCollection");
        IEnumerator IEnumerable.GetEnumerator() => throw Sombra.Erro("TreeNodeCollection");
    }

    public class BindingContext
    {
    }

    public class ListControl : Control
    {
        public object DataSource { get => throw Sombra.Erro("ListControl.DataSource"); set => throw Sombra.Erro("ListControl.DataSource"); }
        public int SelectedIndex { get => throw Sombra.Erro("ListControl.SelectedIndex"); set => throw Sombra.Erro("ListControl.SelectedIndex"); }
        public string ValueMember { get => throw Sombra.Erro("ListControl.ValueMember"); set => throw Sombra.Erro("ListControl.ValueMember"); }
        public string DisplayMember { get => throw Sombra.Erro("ListControl.DisplayMember"); set => throw Sombra.Erro("ListControl.DisplayMember"); }
        public object SelectedValue { get => throw Sombra.Erro("ListControl.SelectedValue"); set => throw Sombra.Erro("ListControl.SelectedValue"); }
        public void BeginUpdate() => throw Sombra.Erro("ListControl.BeginUpdate");
        public void EndUpdate() => throw Sombra.Erro("ListControl.EndUpdate");
    }

    public class ComboBox : ListControl
    {
    }

    public class TreeView : Control
    {
        public void BeginUpdate() => throw Sombra.Erro("TreeView.BeginUpdate");
        public void EndUpdate() => throw Sombra.Erro("TreeView.EndUpdate");
        public TreeNodeCollection Nodes => throw Sombra.Erro("TreeView.Nodes");
        public TreeNode SelectedNode { get => throw Sombra.Erro("TreeView.SelectedNode"); set => throw Sombra.Erro("TreeView.SelectedNode"); }
    }

    public class ListViewItem
    {
        public string Text { get => throw Sombra.Erro("ListViewItem.Text"); set => throw Sombra.Erro("ListViewItem.Text"); }
        public object Tag { get => throw Sombra.Erro("ListViewItem.Tag"); set => throw Sombra.Erro("ListViewItem.Tag"); }
        public ListViewSubItemCollection SubItems => throw Sombra.Erro("ListViewItem.SubItems");

        public class ListViewSubItem
        {
            public string Text { get => throw Sombra.Erro("ListViewSubItem.Text"); set => throw Sombra.Erro("ListViewSubItem.Text"); }
        }

        public class ListViewSubItemCollection
        {
            public ListViewSubItem this[int i] => throw Sombra.Erro("ListViewItem.SubItems[]");
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public Keys KeyCode => throw Sombra.Erro("KeyEventArgs.KeyCode");
        public bool Handled { get => throw Sombra.Erro("KeyEventArgs.Handled"); set => throw Sombra.Erro("KeyEventArgs.Handled"); }
    }

    public class MouseEventArgs : EventArgs
    {
    }

    public class TreeViewEventArgs : EventArgs
    {
        public TreeNode Node => throw Sombra.Erro("TreeViewEventArgs.Node");
    }

    public delegate void KeyEventHandler(object sender, KeyEventArgs e);

    public delegate void MouseEventHandler(object sender, MouseEventArgs e);

    public delegate void TreeViewEventHandler(object sender, TreeViewEventArgs e);

    /// <summary>
    /// Só o que o domínio referencia. ProductName é lido em caminhos de log e de caminho
    /// de arquivo, então devolver valor é mais fiel do que lançar.
    /// </summary>
    public static class Application
    {
        public static string ProductName => "Chummer5a";

        public static string ProductVersion => "5.0.0.0";

        public static string ExecutablePath => StartupPath;

        /// <summary>
        /// Utils.GetStartupPath sai daqui, e dele saem data/, lang/, settings/ e
        /// customdata/. A sondagem roda de um diretório temporário, então a raiz real vem
        /// por variável de ambiente; sem ela, o diretório do executável.
        /// </summary>
        public static string StartupPath
            => Environment.GetEnvironmentVariable("CHUMMER_STARTUP_PATH") ?? AppContext.BaseDirectory;

        public static bool UseWaitCursor { get; set; }

        public static void DoEvents() { }

        public static void Exit()
            => throw Sombra.Erro("Application.Exit");
    }
}

namespace System.Windows.Forms
{
    public class Panel : Control { }

    public class SplitContainer : Control { }

    public class TabPage : Control { }

    public class TabControl : Control
    {
        public IList<TabPage> TabPages => throw Sombra.Erro("TabControl.TabPages");
    }

    public class WebBrowser : Control { }

    public class ColumnHeader : Component
    {
        public string Text { get => throw Sombra.Erro("ColumnHeader.Text"); set => throw Sombra.Erro("ColumnHeader.Text"); }
    }

    public class ListView : Control
    {
        public IList<ListViewItem> Items => throw Sombra.Erro("ListView.Items");
    }

    public class DataGridViewColumn : Component
    {
        public string HeaderText { get => throw Sombra.Erro("DataGridViewColumn.HeaderText"); set => throw Sombra.Erro("DataGridViewColumn.HeaderText"); }
    }

    public class DataGridViewTextBoxColumn : DataGridViewColumn { }

    public class DataGridViewCell
    {
        public object Value { get => throw Sombra.Erro("DataGridViewCell.Value"); set => throw Sombra.Erro("DataGridViewCell.Value"); }
    }

    public class DataGridViewCellCollection
    {
        public DataGridViewCell this[int i] => throw Sombra.Erro("DataGridViewRow.Cells[]");
    }

    public class DataGridViewRow
    {
        public DataGridViewCellCollection Cells => throw Sombra.Erro("DataGridViewRow.Cells");
    }

    public class ToolStripDropDownItem : ToolStripItem
    {
        public ToolStripItemCollection DropDownItems => throw Sombra.Erro("ToolStripDropDownItem.DropDownItems");
    }

    public class ToolStripItemCollection : IEnumerable<ToolStripItem>
    {
        public int Count => throw Sombra.Erro("ToolStripItemCollection.Count");
        public ToolStripItem this[int i] => throw Sombra.Erro("ToolStripItemCollection[]");
        public IEnumerator<ToolStripItem> GetEnumerator() => throw Sombra.Erro("ToolStripItemCollection");
        IEnumerator IEnumerable.GetEnumerator() => throw Sombra.Erro("ToolStripItemCollection");
    }

    public class MenuStrip : ToolStrip { }

    public enum MessageBoxIcon
    {
        None = 0, Error = 16, Question = 32, Warning = 48, Information = 64
    }

    public enum MessageBoxButtons
    {
        OK = 0, OKCancel = 1, AbortRetryIgnore = 2, YesNoCancel = 3, YesNo = 4, RetryCancel = 5
    }

    /// <summary>Só o que CrashHandler enumera por reflexão.</summary>
    public static class SystemInformation
    {
        public static int MonitorCount => 1;

        public static bool TerminalServerSession => false;

        public static string UserName => Environment.UserName;
    }
}
