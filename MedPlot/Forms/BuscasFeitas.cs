using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace MedPlot
{
    public partial class BuscasFeitas : Form
    {
        JanelaPrincipal pai;
        string[] meses = new string[] { "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto",
                                       "Setembro", "Outubro", "Novembro", "Dezembro"};

        public BuscasFeitas(JanelaPrincipal frm1)
        {
            pai = frm1;

            InitializeComponent();

        }

        private void AddQueryNode(string busca)
        {
            if (busca.Length >= 10) // Todo nome padrão de pesquisa do Medplot tem pelo menos 27 caracteres(varia conforme nome do PDC)
                                    // Porem optou-se por deixar deste jeito para que possamos organizar pesquisas por data mas trocando o nome delas.
                                    // Podemos então ter uma pesquisa 20170810_Abertura_LT_x que ela será indexada por data corretamente.
            {
                if (int.TryParse(busca.Substring(0, 4), out int year) && int.TryParse(busca.Substring(4, 2), out int month) && int.TryParse(busca.Substring(6, 2), out int day))
                {
                    TreeNode nAno, nMes, nDia;

                    if (treeBuscas.Nodes.ContainsKey(year.ToString()))
                    {
                        nAno = treeBuscas.Nodes.Find(year.ToString(), false).First(); //Caso já exista um ano, acha-o.
                    }
                    else
                    {
                        nAno = treeBuscas.Nodes.Add(year.ToString(), year.ToString()); //No contrario, cria um novo ano.
                        if (year > int.Parse(treeBuscas.Nodes[0].Name))
                        {
                            treeBuscas.Nodes.Remove(nAno);
                            treeBuscas.Nodes.Insert(0, nAno);
                        }


                    }

                    if (nAno.Nodes.ContainsKey(month.ToString()))
                        nMes = nAno.Nodes.Find(month.ToString(), false).First(); // Igual ao de cima porem para o mês
                    else
                        nMes = nAno.Nodes.Add(month.ToString(), meses[month]); //Igual ao de cima porem para o Dia.


                    if (nMes.Nodes.ContainsKey(day.ToString()))
                        nDia = nMes.Nodes.Find(day.ToString(), false).First(); //Igual ao de cima porem para o Dia.
                    else
                        nDia = nMes.Nodes.Add(day.ToString(), day.ToString()); //Igual ao de cima porem para o Dia.


                    nDia.Nodes.Add(busca, busca.Substring(9)); //Nome final da pesquisa a ser adicionada como node final.
                    nAno.Expand();
                    nMes.Expand();
                    return;
                }

            }
            TreeNode outro;
            if (treeBuscas.Nodes.ContainsKey("outro"))
                outro = treeBuscas.Nodes.Find("outro", false).First(); //Caso já exista um ano, acha-o.
            else
                outro = treeBuscas.Nodes.Add("outro", "Outras Buscas"); //No contrario, cria um novo ano.

            outro.Nodes.Add(busca, busca);



        }

        public void Pastas()
        {
            try
            {
                #region TREEVIEW


                //Criação dos nodes da Tree

                treeBuscas.Nodes.Clear(); //Limpamos os Nodes para garantir que caso o individuo cancele traçar uma busca, não crie novos nodes finais.
                string dirDados = Properties.Settings.Default.QueryFolder;
                DirectoryInfo pasta = new DirectoryInfo(dirDados);
                DirectoryInfo[] subPastas = pasta.GetDirectories();

                foreach (DirectoryInfo dir in subPastas)
                {

                    AddQueryNode(dir.Name);

                }

                //Seleciona pesquisa mais recente

                if (pai.br && pai.nomeAtual != null)
                {
                    TreeNode atual = treeBuscas.Nodes.Find(pai.nomeAtual, true).First();
                    treeBuscas.SelectedNode = atual;
                }

                #endregion

            }
            catch (Exception)
            {

            }

        }

        private void Form2_Activated(object sender, EventArgs e)
        {
            // Atualiza as pastas de consultas realizadas
            Pastas();
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            pai.HabilitaBotao(false);
        }


        private void Form2_CursorChanged(object sender, EventArgs e)
        {
            Pastas();
        }

        private void treeBuscas_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (treeBuscas.SelectedNode.Nodes.Count != 0)
                    return;
                DialogResult escolha; // variável que armazena a escolha do usuário na MessageBox

                escolha = MessageBox.Show("Deseja excluir a pasta de dados: " +
                                         treeBuscas.SelectedNode.Name + " ?",
                                         "Confirmação", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (escolha == System.Windows.Forms.DialogResult.Yes)
                {
                    // Pasta 'Dados'
                    string dirDados = Properties.Settings.Default.QueryFolder;//Parametros.DirDados;
                                                                           // Nome da pasta corrente da qual serão coletados os dados
                    string pastaCorrente = treeBuscas.SelectedNode.Name;
                    // Diretório completo aonde estão os dados da consulta solicitada
                    string nomeCompleto = dirDados + "\\" + pastaCorrente;

                    if (Directory.Exists(nomeCompleto))
                    {
                        // Deleta a pasta selecionada na lista
                        try
                        {
                            Directory.Delete(nomeCompleto, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "ATENÇÃO!", MessageBoxButtons.OK);
                        }
                    }
                    TreeNode pai = treeBuscas.SelectedNode.Parent;
                    treeBuscas.Nodes.Remove(treeBuscas.SelectedNode);
                    checkParent(pai);
                }

            }
        }

        private void checkParent(TreeNode node)
        {
            if (node != null)
            {
                TreeNode pai = node.Parent;
                if (node.Nodes.Count == 0)
                    treeBuscas.Nodes.Remove(node);
                checkParent(pai);
            }
        }

        private void treeBuscas_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (treeBuscas.SelectedNode != null && e.Node.Nodes.Count == 0 && e.Node.Level == 3)
                {
                    pai.selectedQuery = e.Node.Name;

                    // Índice de entrada na lista do form2, se a consulta for aberta vai para a lista de consultas
                    // operantes
                    pai.indEnt = e.Node.Index;
                    

                    DirectoryInfo directoryInfo = new DirectoryInfo(Properties.Settings.Default.QueryFolder + "\\" + e.Node.Name);
                    FileInfo[] files = directoryInfo.GetFiles();

                    if (files.Count() == 0)
                        pai.HabilitaBotao(false);
                    else
                        pai.NovaBuscaAberta();
                }
                else
                {
                    pai.HabilitaBotao(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
