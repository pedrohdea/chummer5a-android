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

// Metade dependente de WinForms de ArmorMod, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Equipment/ArmorMod.cs.
//
// As duas são `partial class ArmorMod`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using NLog;

namespace Chummer.Backend.Equipment
{
    public partial class ArmorMod
    {
        public async Task<TreeNode> CreateTreeNode(ContextMenuStrip cmsArmorMod, ContextMenuStrip cmsArmorGear, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (IncludedInArmor && !string.IsNullOrEmpty(Source) && !await (await _objCharacter.GetSettingsAsync(token).ConfigureAwait(false)).BookEnabledAsync(Source, token).ConfigureAwait(false))
                return null;

            TreeNode objNode = new TreeNode
            {
                Name = InternalId,
                Text = await GetCurrentDisplayNameAsync(token).ConfigureAwait(false),
                Tag = this,
                ContextMenuStrip = string.IsNullOrEmpty(GearCapacity) ? cmsArmorMod : cmsArmorGear,
                ForeColor = await GetPreferredColorAsync(token).ConfigureAwait(false),
                ToolTipText = (await GetNotesAsync(token).ConfigureAwait(false)).WordWrap()
            };

            TreeNodeCollection lstChildNodes = objNode.Nodes;
            await GearChildren.ForEachAsync(async objGear =>
            {
                TreeNode objLoopNode = await objGear.CreateTreeNode(cmsArmorGear, null, token).ConfigureAwait(false);
                if (objLoopNode != null)
                    lstChildNodes.Add(objLoopNode);
            }, token).ConfigureAwait(false);
            if (lstChildNodes.Count > 0)
                objNode.Expand();

            return objNode;
        }
        /// <summary>
        /// Alias map for SourceDetail control text and tooltip assignation.
        /// </summary>
        /// <param name="sourceControl"></param>
        public void SetSourceDetail(Control sourceControl)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            SourceDetail.SetControl(sourceControl);
        }
        public async Task SetSourceDetailAsync(Control sourceControl, CancellationToken token = default)
        {
            if (_objCachedSourceDetail.Language != GlobalSettings.Language)
                _objCachedSourceDetail = default;
            await (await GetSourceDetailAsync(token).ConfigureAwait(false)).SetControlAsync(sourceControl, token).ConfigureAwait(false);
        }
    }
}
