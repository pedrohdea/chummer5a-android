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

// Metade dependente de WinForms de ListItem, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Datastructures/ListItem.cs.
//
// As duas são `partial class ListItem`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

namespace Chummer
{
    public static partial class CompareTreeNodes
    {
        /// <summary>
        /// Sort TreeNodes in alphabetical order, ignoring [].
        /// </summary>
        public static int CompareText(TreeNode tx, TreeNode ty)
        {
            if (tx == null)
            {
                if (ty == null)
                    return 0;
                return -1;
            }
            return ty == null ? 1 : string.Compare(tx.Text.FastEscape('[', ']'), ty.Text.FastEscape('[', ']'), false, GlobalSettings.CultureInfo);
        }
    }

    public static partial class CompareListViewItems
    {
        /// <summary>
        /// Sort ListViewItems in reverse chronological order.
        /// </summary>
        public static int CompareTextAsDates(ListViewItem lx, ListViewItem ly)
        {
            if (lx == null || !DateTime.TryParse(lx.Text, GlobalSettings.CultureInfo, System.Globalization.DateTimeStyles.None, out DateTime datX))
            {
                if (ly == null || !DateTime.TryParse(ly.Text, GlobalSettings.CultureInfo, System.Globalization.DateTimeStyles.None, out _))
                    return 0;
                return -1;
            }

            if (ly == null || !DateTime.TryParse(ly.Text, GlobalSettings.CultureInfo, System.Globalization.DateTimeStyles.None, out DateTime datY))
                return 1;

            return DateTime.Compare(datY, datX);
        }
    }
}
