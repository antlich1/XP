using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;

public class Generator : MonoBehaviour
{
    public int users;
    List<int> numbers;
    public Text textOnGui;
    public InputField inputUsers;
    public int totalNumbers;

    public void SetCount()
    {
        totalNumbers = int.Parse(inputUsers.text) * 5;
    }

    public void GenerateNumbers()
    {
        textOnGui.text = "";
        List<int> numbers = new List<int>();
        SetCount();
        print("count ok");

        for (int i = 1; i <= totalNumbers; i++)
        {
            numbers.Add(i);
        }
        print("numbers generated");

        for (int i = 0; i < totalNumbers; i++)
        {
            int randomIndex = Random.Range(0, totalNumbers);
            int temp = numbers[randomIndex];
            numbers[randomIndex] = numbers[0];
            numbers[0] = temp;
        }
        print("numbers shuffled");

        for (int i = 0; i < numbers.Count; i++)
        {
            textOnGui.text += numbers[i] + ", ";
        }

        print("nubers are on gui");

    }

    public void CSVbutton()
    {
        CSVWriter(GenerateTickets(numbers));
    }

    public void CSVWriter(List<List<int>> tickets)
    {
        string filename = Application.dataPath + "/test.csv";
        TextWriter tw = new StreamWriter(filename, false);
        tw.WriteLine("id, ticket, email");
        tw.Close();

        tw = new StreamWriter(filename, true);

        for (int i = 0; i < tickets.Count; i++)
        {
            for (int j = 0; j<5; j++)
            {
                tw.WriteLine(i + "," + tickets[i][j]);
            }
        }
        tw.Close();
    }
    

    public List<List<int>> GenerateTickets(List<int> numbers)
    {
        List<List<int>> tickets = new List<List<int>>();
        print("created list");
        for (int i = 0; i < numbers.Count/5; i++)
        {
            print("started the cycle");
            for (int j = i*5; j < i + 5; j++)
            { 
                tickets[i][j] = numbers[j];
                print("added the number" + j + " to a ticket" + i);
            }

        }

        for (int i = 0; i < numbers.Count; i++)
        {
            textOnGui.text += tickets[i] + ", ";
        }

        return tickets;
    }
} 