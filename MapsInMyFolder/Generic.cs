using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder
{
    public static class Generic
    {
        public static int CheckIfInputValueHaveChange(UIElement SourcePanel)
        {
            List<System.Type> TypeOfSearchElement = new List<System.Type>
            {
                typeof(TextBox),
                typeof(ComboBox),
                typeof(CheckBox),
                typeof(RadioButton),
                typeof(UserControls.TitleScript),
                typeof(UserControls.ScriptEditor),
                typeof(ICSharpCode.AvalonEdit.TextEditor),
                typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)
            };

            List<UIElement> ListOfisualChildren = Collectif.FindVisualChildren(SourcePanel, TypeOfSearchElement);

            string strHachCode = string.Empty;
            ListOfisualChildren.ForEach(element =>
            {
                if (TypeOfSearchElement.Contains(element.GetType()))
                {
                    string elementXName = element.GetName();
                    if (elementXName != null)
                    {
                        int hachCode = 0;
                        Type type = element.GetType();
                        if (type == typeof(TextBox))
                        {
                            TextBox TextBox = (TextBox)element;
                            string value = TextBox.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(ICSharpCode.AvalonEdit.TextEditor))
                        {
                            ICSharpCode.AvalonEdit.TextEditor TextEditor = (ICSharpCode.AvalonEdit.TextEditor)element;
                            string value = TextEditor.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(ComboBox))
                        {
                            ComboBox ComboBox = (ComboBox)element;
                            string value = ComboBox.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(CheckBox))
                        {
                            CheckBox CheckBox = (CheckBox)element;
                            hachCode = CheckBox.IsChecked.GetHashCode();

                        }
                        else if (type == typeof(RadioButton))
                        {
                            RadioButton RadioButton = (RadioButton)element;
                            hachCode = RadioButton.IsChecked.GetHashCode();
                        }
                        else if (type == typeof(UserControls.TitleScript))
                        {
                            UserControls.TitleScript TitleScript = (UserControls.TitleScript)element;
                            hachCode = TitleScript.ScriptName.GetHashCode();
                            hachCode += TitleScript.Script.GetHashCode();
                        }
                        else if (type == typeof(UserControls.ScriptEditor))
                        {
                            UserControls.ScriptEditor TitleScript = (UserControls.ScriptEditor)element;
                            hachCode = TitleScript.Script.GetHashCode();
                        }
                        else if (type == typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox))
                        {
                            BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox = (BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)element;
                            if (MultiSelectCombobox.SelectedItems != null && MultiSelectCombobox.SelectedItems.Count > 0)
                            {

                                hachCode = string.Join(";", MultiSelectCombobox.SelectedValuesAsString("EnglishName")).GetHashCode();
                            }
                            else
                            {
                                hachCode = 0;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("The type " + type.Name + " is not supported by the function");
                        }
                        strHachCode += hachCode.ToString();
                    }
                }
            });
            ListOfisualChildren.Clear();
            return strHachCode.GetHashCode();
        }
    }
}
