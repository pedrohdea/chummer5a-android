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

// SOMBRAS DO LADO CHUMMER PARA A SONDAGEM EXECUTÁVEL. Ver o cabeçalho de WinFormsShims.cs.
//
// A regra de quando lançar e quando devolver valor:
//
//   ColorManager  DEVOLVE cor. É apresentação pura, e as propriedades PreferredColor do
//                 domínio são lidas em caminhos que nada têm a ver com escolha do usuário.
//                 Lançar aqui produziria falso positivo no resultado do spike.
//   Program       LANÇA no que é janela (MainForm) e devolve no que é estado de processo.
//                 PluginLoader devolve um PluginControl VAZIO. A sondagem provou que o
//                 domínio NÃO trata null: Character.Load itera Program.PluginLoader
//                 .MyActivePlugins sem guarda e estoura. MEF está fora do escopo (PREM-005),
//                 e "nenhum plugin ativo" é o comportamento final do porte — não é máscara.
//   CursorWait    no-op. É cursor de ampulheta; não há cursor.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Plugins;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Chummer
{
    /// <summary>
    /// Sombra de Chummer/Controls/Infrastructure/ColorManager.cs. Devolve cores do modo
    /// claro; o real depende de Registry e de WinForms.
    /// </summary>
    public static class ColorManager
    {
        public static bool IsLightMode => true;

        public static void DisableAutoTimer() { }

        public static Color WindowText => Color.Black;

        public static Color ControlText => Color.Black;

        public static Color GrayText => Color.Gray;

        public static Color ErrorColor => Color.Red;

        public static Color HasNotesColor => Color.SaddleBrown;

        public static Color Control => Color.LightGray;

        public static Color ControlLighter => Color.WhiteSmoke;

        public static Color Window => Color.White;

        public static Color GenerateCurrentModeColor(Color objColor) => objColor;

        public static Color GenerateCurrentModeDimmedColor(Color objColor) => objColor;

        public static Color GenerateDarkModeColor(Color objColor) => objColor;

        public static Color GenerateInverseDarkModeColor(Color objColor) => objColor;

        public static Task SetIsLightModeAsync(bool blnValue, CancellationToken token = default)
            => Task.CompletedTask;

        public static Task AutoApplyLightDarkModeAsync(CancellationToken token = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// Sombra de Chummer/Controls/Infrastructure/CursorWait.cs. Sem ponteiro de mouse, não
    /// há ampulheta a trocar.
    /// </summary>
    public sealed class CursorWait : IDisposable
    {
        public static CursorWait New(object objControl = null, bool blnAppStarting = false,
            CancellationToken token = default) => new CursorWait();

        public static Task<CursorWait> NewAsync(object objControl = null, bool blnAppStarting = false,
            CancellationToken token = default) => Task.FromResult(new CursorWait());

        public void Dispose() { }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Sombra da fachada Chummer/Program.cs. O executável de sondagem não é o aplicativo;
    /// só o que o domínio lê durante a carga precisa existir.
    /// </summary>
    public static class Program
    {
        public static readonly bool IsMainThread = true;

        public static Process MyProcess => Process.GetCurrentProcess();

        // Instância vazia, não null: o domínio faz `Program.PluginLoader?.MyActivePlugins`
        // ao SALVAR mas `Program.PluginLoader.MyActivePlugins` ao CARREGAR — sem proteção.
        // Um aplicativo real tem PluginControl sem plugin ativo, e é isso que a sombra é.
        public static PluginControl PluginLoader { get; } = new PluginControl();

        public static Lazy<TelemetryClient> ChummerTelemetryClient { get; }
            = new Lazy<TelemetryClient>(() => new TelemetryClient());

        public static TelemetryConfiguration ActiveTelemetryConfiguration => null;

        public static ThreadSafeObservableCollection<Character> OpenCharacters { get; }
            = new ThreadSafeObservableCollection<Character>();

        // NULL de propósito: um processo sem janela não tem formulário principal, e é
        // exatamente o caso que o domínio já testa com `if (Program.MainForm != null)`.
        public static ChummerMainForm MainForm => null;

        public static Character LoadCharacter(string strFileName, string strNewName = "",
            bool blnClearFileName = false, bool blnShowErrors = true,
            LoadingBar frmLoadingBar = null, CancellationToken token = default)
            => throw new NotSupportedException(
                "Program.LoadCharacter é a fachada do aplicativo, não do domínio");

        public static Task<Character> LoadCharacterAsync(string strFileName, string strNewName = "",
            bool blnClearFileName = false, bool blnShowErrors = true,
            LoadingBar frmLoadingBar = null, CancellationToken token = default)
            => throw new NotSupportedException(
                "Program.LoadCharacterAsync é a fachada do aplicativo, não do domínio");

        public static Task<bool> SwitchToOpenCharacter(Character objCharacter,
            CancellationToken token = default)
            => Task.FromResult(false);

        public static DialogResult ShowScrollableMessageBox(string message, string caption = null,
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.None,
            System.Windows.Forms.MessageBoxDefaultButton defaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1)
            => throw new NotSupportedException(
                "Caixa de mensagem tocada em execução: " + message);

        public static DialogResult ShowScrollableMessageBox(System.Windows.Forms.Control owner,
            string message, string caption = null,
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.None,
            System.Windows.Forms.MessageBoxDefaultButton defaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1)
            => throw new NotSupportedException(
                "Caixa de mensagem tocada em execução: " + message);

        public static Task<DialogResult> ShowScrollableMessageBoxAsync(string message, string caption = null,
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.None,
            System.Windows.Forms.MessageBoxDefaultButton defaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1,
            CancellationToken token = default)
            => throw new NotSupportedException(
                "Caixa de mensagem tocada em execução: " + message);

        public static Task<DialogResult> ShowScrollableMessageBoxAsync(System.Windows.Forms.Control owner,
            string message, string caption = null,
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.None,
            System.Windows.Forms.MessageBoxDefaultButton defaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1,
            CancellationToken token = default)
            => throw new NotSupportedException(
                "Caixa de mensagem tocada em execução: " + message);

        public static Task<Character> OpenCharacter(Character objCharacter,
            CancellationToken token = default)
            => Task.FromResult(objCharacter);

        public static ThreadSafeForm<LoadingBar> CreateAndShowProgressBar(string strFile = "",
            int intCount = 1) => ThreadSafeForm<LoadingBar>.Get(() => new LoadingBar());

        public static Task<ThreadSafeForm<LoadingBar>> CreateAndShowProgressBarAsync(
            string strFile = "", int intCount = 1, CancellationToken token = default)
            => ThreadSafeForm<LoadingBar>.GetAsync(() => new LoadingBar(), token);
    }
}

namespace Chummer
{
    /// <summary>Sombra de Chummer/Controls/Shared/Components/ElasticComboBox.cs.</summary>
    public class ElasticComboBox : System.Windows.Forms.ComboBox
    {
    }

    /// <summary>Sombra de Chummer/Controls/Shared/Components/NumericUpDownEx.cs.</summary>
    public class NumericUpDownEx : System.Windows.Forms.Control
    {
        public enum InterceptMouseWheelMode
        {
            Always,
            WhenMouseOver,
            WhenFocus,
            Never
        }
    }

    /// <summary>
    /// Extensões de controle que as metades de UI chamam. Todas lançam: um controle
    /// preenchido durante a carga é acoplamento a relatar, não um detalhe a acomodar.
    /// </summary>
    public static class ProbeControlExtensions
    {
        public static System.Windows.Forms.TreeNode FindNode(this System.Windows.Forms.TreeView treTree,
            string strGuid, bool blnDeep = true)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TreeView.FindNode");

        public static System.Windows.Forms.TreeNode FindNode(this System.Windows.Forms.TreeNode objNode,
            string strGuid, bool blnDeep = true)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TreeNode.FindNode");

        public static System.Windows.Forms.TreeNode FindNodeByTag(this System.Windows.Forms.TreeView treTree,
            object objTag, bool blnDeep = true)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TreeView.FindNodeByTag");

        public static System.Windows.Forms.TreeNode FindNodeByTag(this System.Windows.Forms.TreeNode objNode,
            object objTag, bool blnDeep = true)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TreeNode.FindNodeByTag");

        public static void AddOrInsert(this System.Windows.Forms.TreeNodeCollection lstNodes,
            System.Windows.Forms.TreeNode objNode, int intIndex = -1, bool blnSkipAlphabetize = false)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TreeNodeCollection.AddOrInsert");

        public static void SetToolTip(this System.Windows.Forms.Control objControl, string strText)
            => throw new NotSupportedException("UI de WinForms tocada em execução: Control.SetToolTip");

        public static void SetToolTip(this System.Windows.Forms.Control objControl,
            System.Windows.Forms.Control objParent, string strText)
            => throw new NotSupportedException("UI de WinForms tocada em execução: Control.SetToolTip");

        public static Task SetToolTipAsync(this System.Windows.Forms.Control objControl, string strText,
            CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: Control.SetToolTipAsync");

        public static Task SetToolTipAsync(this System.Windows.Forms.Control objControl,
            System.Windows.Forms.Control objParent, string strText, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: Control.SetToolTipAsync");

        public static void PopulateWithListItems(this ElasticComboBox objControl,
            IEnumerable<ListItem> lstItems, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: PopulateWithListItems");

        public static Task PopulateWithListItemsAsync(this ElasticComboBox objControl,
            IEnumerable<ListItem> lstItems, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: PopulateWithListItemsAsync");

        public static void TranslateWinForm(this System.Windows.Forms.Control objControl,
            string strLanguage = "", bool blnDoResumeLayout = true, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TranslateWinForm");

        public static Task TranslateWinFormAsync(this System.Windows.Forms.Control objControl,
            string strLanguage = "", bool blnDoResumeLayout = true, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: TranslateWinFormAsync");

        public static void DoThreadSafe<T>(this T objControl, Action funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafe");

        public static void DoThreadSafe<T>(this T objControl, Action<T> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafe");

        public static void DoThreadSafe<T>(this T objControl, Action<CancellationToken> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafe");

        public static void DoThreadSafe<T>(this T objControl, Action<T, CancellationToken> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafe");

        public static Task DoThreadSafeAsync<T>(this T objControl, Action funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeAsync");

        public static Task DoThreadSafeAsync<T>(this T objControl, Action<T> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeAsync");

        public static Task DoThreadSafeAsync<T>(this T objControl, Action<CancellationToken> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeAsync");

        public static Task DoThreadSafeAsync<T>(this T objControl, Action<T, CancellationToken> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeAsync");

        public static T2 DoThreadSafeFunc<T, T2>(this T objControl, Func<T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFunc");

        public static T2 DoThreadSafeFunc<T, T2>(this T objControl, Func<T, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFunc");

        public static T2 DoThreadSafeFunc<T, T2>(this T objControl, Func<CancellationToken, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFunc");

        public static T2 DoThreadSafeFunc<T, T2>(this T objControl, Func<T, CancellationToken, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFunc");

        public static Task<T2> DoThreadSafeFuncAsync<T, T2>(this T objControl, Func<T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFuncAsync");

        public static Task<T2> DoThreadSafeFuncAsync<T, T2>(this T objControl, Func<T, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFuncAsync");

        public static Task<T2> DoThreadSafeFuncAsync<T, T2>(this T objControl, Func<CancellationToken, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFuncAsync");

        public static Task<T2> DoThreadSafeFuncAsync<T, T2>(this T objControl, Func<T, CancellationToken, T2> funcToRun,
            CancellationToken token = default) where T : System.Windows.Forms.Control
            => throw new NotSupportedException("UI de WinForms tocada em execução: DoThreadSafeFuncAsync");

    }
}

namespace Chummer
{
    /// <summary>Sombra de Chummer/Controls/Infrastructure/ListViewItemWithValue.cs.</summary>
    public class ListViewItemWithValue : System.Windows.Forms.ListViewItem
    {
        public object Value { get; set; }

        public class ListViewSubItemWithValue : System.Windows.Forms.ListViewItem.ListViewSubItem
        {
            public object Value { get; set; }
        }
    }

    /// <summary>Sombra de Chummer/Forms/Character Forms/CharacterShared.cs.</summary>
    public class CharacterShared : System.Windows.Forms.Form
    {
        public Character CharacterObject
            => throw new NotSupportedException("UI de WinForms tocada em execução: CharacterShared.CharacterObject");

        public bool IsDirty
        {
            get => throw new NotSupportedException("UI de WinForms tocada em execução: CharacterShared.IsDirty");
            set => throw new NotSupportedException("UI de WinForms tocada em execução: CharacterShared.IsDirty");
        }

        public Task<bool> SaveCharacter(bool blnPromptUser = true, bool blnDoCreated = false,
            CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: CharacterShared.SaveCharacter");
    }

    /// <summary>Sombra de Chummer/Forms/Utility Forms/CharacterSheetViewer.cs.</summary>
    public class CharacterSheetViewer : System.Windows.Forms.Form
    {
        public ThreadSafeObservableCollection<Character> CharacterObjects
            => throw new NotSupportedException("UI de WinForms tocada em execução: CharacterSheetViewer.CharacterObjects");
    }

    /// <summary>Sombra de Chummer/Forms/Utility Forms/ExportCharacter.cs.</summary>
    public class ExportCharacter : System.Windows.Forms.Form
    {
        public Character CharacterObject
            => throw new NotSupportedException("UI de WinForms tocada em execução: ExportCharacter.CharacterObject");
    }

    /// <summary>
    /// Sombra de Chummer/Forms/ChummerMainForm.cs. Program.MainForm devolve NULL nesta
    /// sondagem, que é a resposta honesta para um processo sem janela — e é o caso que o
    /// domínio já testa com `if (Program.MainForm != null)`. O tipo existe só para as
    /// assinaturas vincularem.
    /// </summary>
    public class ChummerMainForm : System.Windows.Forms.Form
    {
        public ThreadSafeObservableCollection<CharacterShared> OpenCharacterEditorForms
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public ThreadSafeObservableCollection<CharacterSheetViewer> OpenCharacterSheetViewers
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public ThreadSafeObservableCollection<ExportCharacter> OpenCharacterExportForms
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public IReadOnlyList<IHasCharacterObjects> OpenFormsWithCharacters
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public System.Windows.Forms.MenuStrip MainMenuStrip
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public System.Windows.Forms.Form[] MdiChildren
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public System.Windows.Forms.Form PrintMultipleCharactersForm
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public System.Windows.Forms.Form CharacterRoster
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public System.Windows.Forms.Form MasterIndex
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public void RefreshAllTabTitles(CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public Task RefreshAllTabTitlesAsync(CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public Task<bool> AnyOpenFormContainsCharacter(Character objCharacter,
            System.Windows.Forms.Form objExclude = null, CancellationToken token = default)
            => Task.FromResult(false);

        public void TranslateWinForm()
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");

        public Task TranslateWinFormAsync(bool blnDoResumeLayout = true, CancellationToken token = default)
            => throw new NotSupportedException("UI de WinForms tocada em execução: ChummerMainForm");
    }
}

namespace Chummer.Properties
{
    /// <summary>Sombra de Chummer/Properties/Settings.settings, lida só pelo CrashHandler.</summary>
    public sealed class Settings
    {
        public static Settings Default { get; } = new Settings();

        public Guid UploadClientId { get; set; } = Guid.Empty;
    }
}

namespace Chummer.Forms
{
    /// <summary>
    /// Sombra de Chummer/Forms/DummyForm.cs. Utils cria uma para que o WinForms instale um
    /// SynchronizationContext antes de montar o JoinableTaskContext. Sem WinForms não há
    /// contexto a instalar, e o descartável vazio preserva a forma do using.
    /// </summary>
    public sealed class DummyForm : IDisposable
    {
        public void Dispose() { }
    }
}

namespace Chummer
{
    /// <summary>Sombra de Chummer/Telemetry/ApplicationInsights/UploadObjectAsMetric.cs.</summary>
    public static class UploadObjectAsMetric
    {
        public static bool UploadObject(Microsoft.ApplicationInsights.TelemetryClient objClient,
            object objToUpload) => true;

        public static bool UploadObject(Microsoft.ApplicationInsights.TelemetryClient objClient,
            Type objType) => true;
    }
}
