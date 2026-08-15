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

// Metade dependente de WinForms de SourceString, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Datastructures/SourceString.cs.
//
// As duas são `partial class SourceString`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Threading;

namespace Chummer
{
    public readonly partial struct SourceString
    {
        /// <summary>
        /// Set the Text and ToolTips for the selected control.
        /// </summary>
        public void SetControl(Control source)
        {
            if (source == null)
                return;
            string strText = ToString();
            source.DoThreadSafe(x => x.Text = strText);
            source.SetToolTip(LanguageBookTooltip);
        }
        /// <summary>
        /// Set the Text and ToolTips for the selected control.
        /// </summary>
        public void SetControl(Control source, Form frmParent)
        {
            if (source == null)
                return;
            string strText = ToString();
            source.DoThreadSafe(x => x.Text = strText);
            string strToolTip = LanguageBookTooltip;
            if (source is IControlWithToolTip objSourceWithToolTip)
                objSourceWithToolTip.ToolTipText = strToolTip;
            else
                source.SetToolTip(frmParent, strToolTip);
        }
        /// <summary>
        /// Set the Text and ToolTips for the selected control.
        /// </summary>
        public async Task SetControlAsync(Control source, CancellationToken token = default)
        {
            if (source == null)
                return;
            string strText = await ToStringAsync(token).ConfigureAwait(false);
            await source.DoThreadSafeAsync(x => x.Text = strText, token).ConfigureAwait(false);
            await source.SetToolTipAsync(await GetLanguageBookTooltipAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
        }
        /// <summary>
        /// Set the Text and ToolTips for the selected control.
        /// </summary>
        public async Task SetControlAsync(Control source, Form frmParent, CancellationToken token = default)
        {
            if (source == null)
                return;
            string strText = await ToStringAsync(token).ConfigureAwait(false);
            await source.DoThreadSafeAsync(x => x.Text = strText, token).ConfigureAwait(false);
            string strToolTip = await GetLanguageBookTooltipAsync(token).ConfigureAwait(false);
            if (source is IControlWithToolTip objSourceWithToolTip)
                await objSourceWithToolTip.SetToolTipTextAsync(strToolTip, token).ConfigureAwait(false);
            else
                await source.SetToolTipAsync(frmParent, strToolTip, token).ConfigureAwait(false);
        }
    }
}
