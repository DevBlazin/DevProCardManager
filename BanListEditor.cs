﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

namespace DevPro_CardManager
{
    public sealed partial class BanListEditor : Form
    {
        public BanListEditor()
        {
            InitializeComponent();
            TopLevel = false;
            Dock = DockStyle.Fill;
            Visible = true;
            LoadBanList();
     
            BanList.SelectedIndexChanged += BanList_SelectedIndexChanged;
            if (BanList.Items.Count > 0)
                BanList.SelectedIndex = 0;

            BannedList.AllowDrop = true;
            LimitedList.AllowDrop = true;
            SemiLimitedList.AllowDrop = true;
            
            SearchBox.List.MouseDown += SearchList_MouseDown;
            BannedList.DragEnter += List_DragEnter;
            LimitedList.DragEnter += List_DragEnter;
            SemiLimitedList.DragEnter += List_DragEnter;
            BannedList.DragDrop += List_DragDrop;
            LimitedList.DragDrop += List_DragDrop;
            SemiLimitedList.DragDrop += List_DragDrop;
            BannedList.DrawItem += List_DrawItem;
            LimitedList.DrawItem += List_DrawItem;
            SemiLimitedList.DrawItem += List_DrawItem;

            BanListInput.Enter += BanListInput_Enter;
            BanListInput.Leave += BanListInput_Leave;
            BanListInput.KeyDown += BanListInput_KeyDown;

            LimitedList.KeyDown += DeleteItem;
            SemiLimitedList.KeyDown += DeleteItem;
            BannedList.KeyDown += DeleteItem;
            BanList.KeyDown += DeleteBanList;

        }

        Dictionary<string, List<BanListCard>> m_banlists;

        private void LoadBanList()
        {
            m_banlists = new Dictionary<string, List<BanListCard>>();
            if (!File.Exists("lflist.conf"))
                return;

            var reader = new StreamReader(File.OpenRead("lflist.conf"));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)|| line.StartsWith("#")) continue;
                if (line.StartsWith("!"))
                {
                    if (!BanList.Items.Contains(line.Substring(1)))
                        BanList.Items.Add(line.Substring(1));
                }
                else
                {
                    string[] parts = line.Split(' ');
                    if (!Program.CardData.ContainsKey(Int32.Parse(parts[0])))
                        continue;

                    if (Program.CardData[Int32.Parse(parts[0])].Name == "")
                        continue;


                    if (!m_banlists.ContainsKey(BanList.Items[BanList.Items.Count - 1].ToString()))
                    {
                        
                        m_banlists.Add(BanList.Items[BanList.Items.Count - 1].ToString(), new List<BanListCard>());
                        m_banlists[BanList.Items[BanList.Items.Count - 1].ToString()].Add(
                            new BanListCard { ID = Int32.Parse(parts[0]), Banvalue = Int32.Parse(parts[1]), Name = Program.CardData[Int32.Parse(parts[0])].Name });
                    }
                    else
                    {
                        if (!m_banlists[BanList.Items[BanList.Items.Count - 1].ToString()].Exists(banListCard => banListCard.ID == Int32.Parse(parts[0])))
                            m_banlists[BanList.Items[BanList.Items.Count - 1].ToString()].Add(
                                new BanListCard { ID = Int32.Parse(parts[0]), Banvalue = Int32.Parse(parts[1]), Name = Program.CardData[Int32.Parse(parts[0])].Name });
                    }
                }
            }
            reader.Close();
        }

        private void SaveBanList()
        {
            using (var writer = new StreamWriter("lflist.conf", false))
            {
                writer.WriteLine("#Built using DevPro card editor.");
                foreach (object t in BanList.Items)
                {
                    writer.WriteLine("!{0}", t);
                    try
                    {
                        var forbidden = m_banlists[t.ToString()].FindAll(x => x.Banvalue == 0);
                        var limited = m_banlists[t.ToString()].FindAll(x => x.Banvalue == 1);
                        var semiLimited = m_banlists[t.ToString()].FindAll(x => x.Banvalue == 2);

                        writer.WriteLine("#forbidden");
                        foreach (var banListCard in forbidden)
                        {
                            writer.WriteLine("{0} {1}", banListCard.ID, banListCard.Banvalue);
                        }

                        writer.WriteLine("#limit");
                        foreach (var banListCard in limited)
                        {
                            writer.WriteLine("{0} {1}", banListCard.ID, banListCard.Banvalue);
                        }

                        writer.WriteLine("#semi limit");
                        foreach (var banListCard in semiLimited)
                        {
                            writer.WriteLine("{0} {1}", banListCard.ID, banListCard.Banvalue);
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        Debug.WriteLine("Unlimited was probably hit, good idea to check it out.");
                    }
                }
            }

            MessageBox.Show("Save Complete");

        }

        private void SearchList_MouseDown(object sender, MouseEventArgs e)
        {
            var list = (ListBox)sender;
            int indexOfItem = list.IndexFromPoint(e.X, e.Y);
            if (indexOfItem >= 0 && indexOfItem < list.Items.Count)
            {
                list.DoDragDrop(list.Items[indexOfItem], DragDropEffects.Copy);
            }
        }
        private void List_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy; 
        }
        private void List_DragDrop(object sender, DragEventArgs e)
        {
            var list = (ListBox)sender;
            int indexOfItemUnderMouseToDrop = list.IndexFromPoint(list.PointToClient(new Point(e.X, e.Y)));
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                if (!BannedList.Items.Contains(e.Data.GetData(DataFormats.Text)) && !LimitedList.Items.Contains(e.Data.GetData(DataFormats.Text))
                        && !SemiLimitedList.Items.Contains(e.Data.GetData(DataFormats.Text)))
                {
                    if (indexOfItemUnderMouseToDrop >= 0 && indexOfItemUnderMouseToDrop < list.Items.Count)
                        list.Items.Insert(indexOfItemUnderMouseToDrop, e.Data.GetData(DataFormats.Text));
                    else
                        list.Items.Add(e.Data.GetData(DataFormats.Text));
                }
                else
                {
                    int cardid = Int32.Parse((string)e.Data.GetData(DataFormats.Text));
                    if (BannedList.Items.Contains(e.Data.GetData(DataFormats.Text)))
                        MessageBox.Show(Program.CardData[cardid].Name + " is already contained in the Banned list.");
                    else if (LimitedList.Items.Contains(e.Data.GetData(DataFormats.Text)))
                        MessageBox.Show(Program.CardData[cardid].Name + " is already contained in the Limited list.");
                    else if (SemiLimitedList.Items.Contains(e.Data.GetData(DataFormats.Text)))
                        MessageBox.Show(Program.CardData[cardid].Name + " is already contained in the SemiLimited list.");
                }
            }
        } 

        private void BanList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BanList.SelectedItem == null) return;
            BannedList.Items.Clear();
            LimitedList.Items.Clear();
            SemiLimitedList.Items.Clear();
            if (m_banlists.ContainsKey(BanList.SelectedItem.ToString()))
            {
                foreach (BanListCard card in m_banlists[BanList.SelectedItem.ToString()])
                {
                    if (card.Banvalue == 0)
                        BannedList.Items.Add(card.ID);
                    else if (card.Banvalue == 1)
                        LimitedList.Items.Add(card.ID);
                    else if (card.Banvalue == 2)
                        SemiLimitedList.Items.Add(card.ID);
                }
            }
        }

        private void List_DrawItem(object sender, DrawItemEventArgs e)
        {
            var list = (ListBox)sender;
            e.DrawBackground();

            bool selected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);

            int index = e.Index;
            if (index >= 0 && index < list.Items.Count)
            {
                string text = list.Items[index].ToString();
                Graphics g = e.Graphics;
                if (!Program.CardData.ContainsKey(Int32.Parse(text)))
                    list.Items.Remove(text);
                else
                {
                    CardInfos card = Program.CardData[Int32.Parse(text)];

                    g.FillRectangle((selected) ? new SolidBrush(Color.Blue) : new SolidBrush(Color.White), e.Bounds);

                    // Print text
                    g.DrawString((card.Name == "" ? card.Id.ToString(CultureInfo.InvariantCulture) : card.Name), e.Font,
                                 (selected) ? Brushes.White : Brushes.Black,
                                 list.GetItemRectangle(index).Location);
                }
            }

            e.DrawFocusRectangle();
        }

        private void BanAnimeCardsBtn_Click(object sender, EventArgs e)
        {
            foreach (int id in Program.CardData.Keys)
            {
                if (Program.CardData[id].Ot == 4)
                    if (!BannedList.Items.Contains(id))
                    {
                        BannedList.Items.Add(id);
                        m_banlists[BanList.Items[BanList.Items.Count - 1].ToString()].Add(
                            new BanListCard { ID = id, Banvalue = 0, Name = Program.CardData[id].Name });
                    }
            }
        }

        private void Savebtn_Click(object sender, EventArgs e)
        {
            SaveBanList();
        }

        private void Clearbtn_Click(object sender, EventArgs e)
        {
            BannedList.Items.Clear();
            LimitedList.Items.Clear();
            SemiLimitedList.Items.Clear();
            m_banlists[BanList.SelectedItem.ToString()].Clear();
        }
        private void BanListInput_Enter(object sender, EventArgs e)
        {
            if (BanListInput.Text == "Add BanList")
            {
                BanListInput.Text = "";
                BanListInput.ForeColor = SystemColors.WindowText;
            }
        }

        private void BanListInput_Leave(object sender, EventArgs e)
        {
            if (BanListInput.Text == "")
            {
                BanListInput.Text = "Add BanList";
                BanListInput.ForeColor = SystemColors.WindowFrame;
            }
        }
        private void BanListInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(BanListInput.Text))
                    return;

                if (m_banlists.ContainsKey(BanListInput.Text))
                {
                    BanList.SelectedItem = BanListInput.Text;
                    return;
                }

                m_banlists.Add(BanListInput.Text,new List<BanListCard>());
                BanList.Items.Add(BanListInput.Text);
                BanList.SelectedItem = BanListInput.Text;
                BanListInput.Clear();
            }
        }

        private void DeleteItem(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                var list = (ListBox) sender;
                if(list.SelectedIndex != -1)
                    list.Items.RemoveAt(list.SelectedIndex);
            }
        }

        private void DeleteBanList(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                var list = (ListBox)sender;
                if (list.SelectedIndex != -1)
                {
                    Clearbtn_Click(null,null);
                    m_banlists.Remove(list.SelectedItem.ToString());
                    list.Items.RemoveAt(list.SelectedIndex);
                }
            }
        }
    }
    public class BanListCard
    {
        public int ID;
        public string Name;
        public int Banvalue;
    }
}
