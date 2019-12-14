using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;
using System.Threading;


enum Modes
{
    E_MAIN_MODE = 0,
    E_STIB_MODE,
    E_VILLO_MODE,
    E_SCOOTY_MODE,
    E_SNCB_MODE,
    E_METEO_MODE,
    E_TRAFIC_MODE,
    E_SELECT_TRAM
}

enum TypeButtons
{
    E_CHOICE_TYPE,
    E_BACK_TYPE,
}


public class Script_Serial : MonoBehaviour
{
    // Start is called before the first frame update
    private const int C_N_OF_TRAMS = 18;
    private const int C_N_OF_BUTTONS_CHOICE = 6;
    private const int C_N_OF_BUTTONS = 1+C_N_OF_BUTTONS_CHOICE;
    private const int C_BUTTON_BACK     = C_N_OF_BUTTONS-1;

    private bool _looping;
    private SerialPort _port;
    private Thread portReadingThread;
    private uint ui_Data;
    

    private bool Data_Received = false;

    private Modes mode = Modes.E_MAIN_MODE;
    private TypeButtons button;
    private uint idx_tram_lines;
    private float x_origin = 189;
    private float x_distance = 46;

    public uint[] ui_tram_lines     = new uint[C_N_OF_TRAMS];
    public Sprite[] img_Tram_Lines  = new Sprite[C_N_OF_TRAMS];
    public Sprite[] img_3Trams  = new Sprite[ (C_N_OF_TRAMS/3) ];
    public Sprite[] img_modes   = new Sprite[C_N_OF_BUTTONS_CHOICE];
    public Image[] img_button = new Image[C_N_OF_BUTTONS_CHOICE];
    public Image[] img_Highlight_button = new Image[C_N_OF_BUTTONS];

    void Start()
    {

        uint uc_i;
        Init_port();
        Place_Objects();
        for (uc_i = 0; uc_i < C_N_OF_BUTTONS_CHOICE; uc_i++)
        {
            img_button[uc_i].sprite = img_modes[uc_i];
            img_button[uc_i].enabled = true;
            img_Highlight_button[uc_i].enabled = true;
        }
        /*Unhighlight back button*/
        img_Highlight_button[uc_i].enabled = false  ;

        _looping = true;
        portReadingThread = new Thread(Read_Bus);
        portReadingThread.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (Data_Received)
        {
            Handle_Information();
            Data_Received = false;
        }
    }

    void Place_Objects()
    {
        uint ui_i;
        float x;

        Vector3 temp = new Vector3(0,0,0);
        
        for (ui_i = 0; ui_i < C_N_OF_BUTTONS_CHOICE; ui_i++)
        {
            x = x_origin + (float)(ui_i) * x_distance;
            x -= img_Highlight_button[ui_i].rectTransform.position.x;
            temp.x = x;
            img_button[ui_i].transform.position += temp;
        }

        for (ui_i = 0; ui_i < C_N_OF_BUTTONS; ui_i++)
        {
            x = x_origin + (float)(ui_i) * x_distance;
            x -= img_Highlight_button[ui_i].rectTransform.position.x;
            temp.x = x;
            img_Highlight_button[ui_i].transform.position += temp;
            print(x);

        }

    }
    private void OnDestroy()
    {
        _looping = false;  // This is a necessary command to stop the thread.
                           // if you comment this line, Unity gets frozen when you stop the game in the editor.                           
        portReadingThread.Join(1);
        portReadingThread.Abort();
        _port.Close();
    }

    void Handle_Information()
    {
        uint uc_i;
        Get_Type_Button_pressed(ui_Data);

        Debug.Log(button.ToString());
        Debug.Log(mode.ToString());
        switch (mode)
        {
            case Modes.E_MAIN_MODE:
                if (button == TypeButtons.E_CHOICE_TYPE)
                Get_mode_From_button();
                break;

            case Modes.E_STIB_MODE:

                if (button == TypeButtons.E_CHOICE_TYPE)
                {
                    Show3tramLines((uint)(ui_Data));
                    mode = Modes.E_SELECT_TRAM;
                }
                break;

            case Modes.E_SELECT_TRAM:

                if (button == TypeButtons.E_CHOICE_TYPE)
                {
                    //TODO: Renvoyer le numéro de tram vers le programme
                    //Remettre en MAIN ou laisser en STIB jusque quand back ?
                    GoToMainMode();
                    print(ui_Data);
                }
                else if (button == TypeButtons.E_BACK_TYPE)
                {
                    mode = Modes.E_STIB_MODE;
                    for (uc_i = 0; uc_i < (C_N_OF_TRAMS / 3); uc_i++)
                    {
                        img_button[uc_i].sprite = img_3Trams[uc_i];
                        img_button[uc_i].enabled = true;
                    }
                    
                    for (uc_i = 0; uc_i < C_N_OF_BUTTONS; uc_i++)
                    {
                        img_Highlight_button[uc_i].enabled = true; 
                    }
                    return;
                }

                break;
            case Modes.E_VILLO_MODE:
                if (button == TypeButtons.E_CHOICE_TYPE)
                    Get_mode_From_button();
                break;
            
            case Modes.E_SCOOTY_MODE:
                if (button == TypeButtons.E_CHOICE_TYPE)
                    Get_mode_From_button();
                break;
            
            case Modes.E_SNCB_MODE:
                if (button == TypeButtons.E_CHOICE_TYPE)
                    Get_mode_From_button();
                break;
            
            case Modes.E_METEO_MODE:
                break;
            
            case Modes.E_TRAFIC_MODE:
                if (button == TypeButtons.E_CHOICE_TYPE)
                    Get_mode_From_button();
                break;
        }
        if (button == TypeButtons.E_BACK_TYPE)
        {
            GoToMainMode();
        }
    }

    void GoToMainMode()
    {
        uint uc_i;
        mode = Modes.E_MAIN_MODE;

        for (uc_i = 0; uc_i < C_N_OF_BUTTONS_CHOICE; uc_i++)
        {
            img_button[uc_i].enabled = true;
            img_button[uc_i].sprite = img_modes[uc_i];
            img_Highlight_button[uc_i].enabled = true;
        }

        img_Highlight_button[uc_i].enabled = false;

    }

    void Show3tramLines(uint ui_tram_line)
    {
        uint uc_i;
        for (uc_i = 0; uc_i < 3; uc_i++)
        {
            img_button[uc_i].sprite = img_Tram_Lines[uc_i+(3*ui_tram_line)];
            img_button[uc_i].enabled = true;

            img_Highlight_button[uc_i].enabled = true; 
        }
        for (uc_i = uc_i; uc_i < C_N_OF_BUTTONS_CHOICE; uc_i++)
        {
            img_button[uc_i].enabled = false;

            img_Highlight_button[uc_i].enabled = false; 
        }
    }

    void Get_mode_From_button()
    {
        uint uc_i;
        
        switch (ui_Data)
        {
            case 0:
                mode = Modes.E_STIB_MODE;
                for (uc_i = 0; uc_i < (C_N_OF_TRAMS/3); uc_i++)
                {
                    img_button[uc_i].sprite = img_3Trams[uc_i];
                    img_button[uc_i].enabled = true;
                }

                for (uc_i = 0; uc_i < C_N_OF_BUTTONS; uc_i++)
                {

                    img_Highlight_button[uc_i].enabled = true; 
                }

                idx_tram_lines = 0;
                break;
            case 1:
                mode = Modes.E_VILLO_MODE ;
                break;
            case 2:
                mode = Modes.E_SCOOTY_MODE;
                break;
            case 3:
                mode = Modes.E_SNCB_MODE;
                break;
            case 4:
                mode = Modes.E_METEO_MODE;
                break;
            case 5:
                mode = Modes.E_TRAFIC_MODE;
                break;
        }

        //Update_Led_Buttons();
    }


    void Get_Type_Button_pressed(uint pressed_button)
    {
        uint uc_i;
        //TODO: Mofifier ici pour avoir de meilleures choses avec les boutons
        switch (pressed_button)
        {
            case C_BUTTON_BACK:
                button = TypeButtons.E_BACK_TYPE;
                for (uc_i = 0; uc_i < C_N_OF_BUTTONS_CHOICE; uc_i++)
                {
                    img_button[uc_i].sprite = img_modes[uc_i];
                    img_button[uc_i].enabled = true;
                }
                break;
            default:
                button = TypeButtons.E_CHOICE_TYPE;
                break;
        }
    }

    void Init_port()
    {
        // COM number larger than 9, add prefix \\\\.\\. 
        _port = new SerialPort()
        {
            PortName = "COM5",
            BaudRate = 9600,
        };

        //_port = new SerialPort("COM3", 9600);
        _port.Open();
        _port.DiscardInBuffer();
        _port.DiscardOutBuffer();

        if (_port == null)
        {
            Debug.LogError("_port is null");
            return;
        }
    }

    void Read_Bus()
    {
        // Start reading the data coming through the serial port.
        _port.DiscardInBuffer();
        while (_looping)
        {
            if (_port.IsOpen)
            {
                ui_Data = (uint)_port.ReadChar(); // blocking call.
                print(_port.BytesToRead);
                if (ui_Data <= 10)//parce qu'on recoit 63. modifier
                {
                    //print(ui_Data);
                    Data_Received = true;
                }
                else
                {
                    print(ui_Data);
                }
            }
            Thread.Sleep(0);
        }
    }
}
