using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Threading.Tasks;
using System;

public class Lottery : MonoBehaviour
{
    List<int> numbersBag = new List<int>();
    List<int> lastNumbers = new List<int>();
    List<List<int>> tickets = new List<List<int>>();
    List<string> winners = new List<string>();
    List<string> allWinners = new List<string>();
    List<List<string>> prizeWinners = new List<List<string>>();
    List<Text> winnersListGui = new List<Text>();
    List<int> howMuchWinners = new List<int>();
    
    public ParticleSystem numberEffect;
    public Text winnerGUI;
    public Text allWinnerGUI;
    public Text newNumberGUI;
    public Text numbersStoryGUI;

    public Animator numbersAnimator;
    
    public Text winner5000;
    public Text winner3000;
    public Text winner1000;
    public Text winner500;
    
    int members = 95;
    float timer = 0f;
    bool victory;
    int counter = 0;
    /*private void Update()
    {
        timer += Time.deltaTime;
        if (!victory && timer > 0.05f)
        {
            NewNumber();
            timer = 0f;
        }
    }*/

    private void Start()
    {

        Application.runInBackground = true;
        for (int i = 0; i < winnersListGui.Count; i++)
        {
            winnersListGui[i].text = "";
        }


        allWinnerGUI.text = "";

        winnersListGui.Add(winner5000);
        winnersListGui.Add(winner3000);
        winnersListGui.Add(winner1000);
        winnersListGui.Add(winner500);
        
        howMuchWinners.Add(5);
        howMuchWinners.Add(10);
        howMuchWinners.Add(20);
        howMuchWinners.Add(30);

        for (int i = 0; i < 4; i++)
        {
            prizeWinners.Add(new List<string>());
        }
        newNumberGUI.text = "";
        numbersStoryGUI.text = "";
        winners.Clear();
        for (int i = 1; i < 100; i++)
        {
            numbersBag.Add(i);
        }

        
        ReadMembers();
        
        /*
        for (int i = 0; i < members; i++)
        {
            tickets.Add(new List<int>());
            for (int j = 0; j < 5; j++)
            {
                int t = UnityEngine.Random.Range(0, numbersBag.Count);
                tickets[i].Add(numbersBag[t]);
            }
        }
        
        */
        /*
        List<LotoMember> testMembers = ReadMembers();
        for (int i = 0; i < testMembers.Count; i++) 
        {
            print(testMembers[i].id);
            print(testMembers[i].ticket);
            print(testMembers[i].email);
            print(testMembers[i].name);
            print("______________");

        }
        */

        //WriteTickets();
    }
    public void NewNumber()
    {

        numbersAnimator.SetTrigger("newNum");

        winnerGUI.text = "";
        victory = false;
        if (numbersBag.Count >= 1)
        {
            counter++;
            print(counter);
            int randomIndex = UnityEngine.Random.Range(0, numbersBag.Count);
            int temp = numbersBag[randomIndex];
            newNumberGUI.text = temp.ToString();
            lastNumbers.Add(temp);
            if (lastNumbers.Count >= 10)
            {
                lastNumbers.RemoveAt(0);
            }
            numbersStoryGUI.text = "";
            for (int i = 0; i < lastNumbers.Count; i++)
            {
                numbersStoryGUI.text += lastNumbers[i].ToString() + " ";
            }
            numbersBag.RemoveAt(randomIndex);
            for (int i = 0; i < tickets.Count; i++)
            {
                tickets[i].RemoveAll(item => item == temp);
            }

            for (int i = 0; i < tickets.Count; i++)
            {
                if (tickets[i].Count < 1)
                {
                    Victory();
                    return;
                }
            }
        }
        else
        {
            newNumberGUI.text = "=)";
        }
    }
    public void Victory()
    {
        allWinners.Clear();

        for (int i = 0; i < winnersListGui.Count; i++)
        {
            winnersListGui[i].text = "";
        }

        List<string> winners = new List<string>();
        for (int i = 0; i < tickets.Count; i++)
        {
            if (tickets[i].Count < 1)
            {
                winners.Add(i.ToString());
                allWinners.Add(i.ToString());
                tickets[i].Add(-1);

                for (int j = 0; j < prizeWinners.Count; j++)
                {
                    if (prizeWinners[j].Count < howMuchWinners[j])
                    {
                        prizeWinners[j].Add(i.ToString());
                        j = prizeWinners.Count;
                    }
                }

            }
        }

        for (int i = 0; i < allWinners.Count; i++)
        {
            allWinnerGUI.text += allWinners[i] + ", ";
        }
        if (winners.Count > 1)
        {
            winnerGUI.text = "Обнаружены победители!";
        }
        else
        {
            winnerGUI.text = "Обнаружен победитель!";
        }
        for (int i = 0; i < winners.Count; i++)
        {
            winnerGUI.text += "\n" + "Билет #" + winners[i];
        }
        winnersToGui();
        victory = true;

        for (int i = 0; i < prizeWinners.Count; i++) 
        {
            print(i + " winner");
            for (int j = 0; j <prizeWinners[i].Count; j++)
            {
                print(prizeWinners[i][j]);
            }
        }


    }
    public void winnersToGui()
    {
        
        for (int i = 0; i<prizeWinners.Count; i++)
        {
            int tempWinners = prizeWinners[i].Count;
            for (int j = 0; j < tempWinners; j++)
            {
                winnersListGui[i].text += prizeWinners[i][j]+ " ";
            }
        }
    }

    public void WriteTickets()
    {
        string filename = @"lotomembers.csv";
        TextWriter tw = new StreamWriter(filename, false);
        
        for (int i = 0; i < tickets.Count; i++)
        {
            tw.Write(i+"; ");
            for (int j = 0; j < tickets[i].Count; j++)
            {
                tw.Write(tickets[i][j]);
                if (j < tickets[i].Count-1)
                {
                    tw.Write("; ");
                }

            }
            tw.WriteLine();
        }
        tw.Close();
    }



    /*
    class LotoMember
    {
        public int id;
        public string ticket;
        public string email;
        public string name;
    }

    */

    public void ReadMembers() {

        for (int i = 0; i < members; i++)
        {
            tickets.Add(new List<int>());
        }
        using (var reader = new StreamReader(@"C:\lotomembers.csv"))
        {

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(';');

                for (int i = 0; i < values.Length; i++)
                {
                    print(values[i]);
                }

                if (values.Length > 0)
                {
                    for (int i = 0; i < tickets.Count; i++)
                    {
                        for (int j = 1; j < values.Length; j++)
                        {
                            tickets[int.Parse(values[0])].Add(int.Parse(values[j]));
                            print("done " + i);
                        }
                    }
                    
                }
            }
        }
    }
}