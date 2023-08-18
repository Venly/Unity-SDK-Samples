using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public InputField NicknameInputField;
    public InputField EmailInputField;
    public InputField PasswordInputField;
    public InputField PinInputField;
    public Button LoginButton;
    public Button CreateButton;

    public Button DemoButton;

    public Button SignUpButton;
    public Button SignUpBackButton;

    void Start()
    {
        OpenLoginUI();

        SignUpButton.onClick.AddListener(OpenSignUpUI);
        SignUpBackButton.onClick.AddListener(OpenLoginUI);

        LoginButton.onClick.AddListener(SignIn);
        CreateButton.onClick.AddListener(SignUp);
        DemoButton.onClick.AddListener(Demo);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SignUpButton.onClick.RemoveListener(OpenSignUpUI);
        SignUpBackButton.onClick.RemoveListener(OpenLoginUI);

        LoginButton.onClick.RemoveListener(SignIn);
        CreateButton.onClick.RemoveListener(SignUp);
        DemoButton.onClick.RemoveListener(Demo);
    }


    protected internal void SetFirstTimeUserExperience()
    {
        if (PlayerDataWeb3.instance.ftueLevel == 0)
        {
            PlayerDataWeb3.instance.ftueLevel = 1;
            PlayerDataWeb3.instance.SaveLocalData();
        }
    }

    protected internal void SignIn()
    {
        SetFirstTimeUserExperience();

        //SHOW LOADER
        Web3Controller.ShowLoader("Signing in");

        //PERFORM LOGIN/DEMO/SIGNUP
        //#if ENABLE_VENLY_DEVMODE
        //        Web3Controller.SignIn(null,null)
        //#else
        Web3Controller.SignIn(EmailInputField.text,PasswordInputField.text)
            //#endif
            .OnSuccess(() =>
            {
                SceneManager.LoadScene("main");
            })
            .OnFail(Web3Controller.ShowException)
            .Finally(Web3Controller.HideLoader);
    }

    protected internal void SignUp()
    {
        SetFirstTimeUserExperience();

        Web3Controller.ShowLoader("Signing Up");

        Web3Controller.SignUp(EmailInputField.text, PasswordInputField.text,PinInputField.text, NicknameInputField.text)
            .OnSuccess(() =>
            {
                SceneManager.LoadScene("main");
            })
            .OnFail(Web3Controller.ShowException)
            .Finally(Web3Controller.HideLoader);
    }

    protected internal void Demo()
    {
        SetFirstTimeUserExperience();

        Web3Controller.ShowLoader("Loading demo data");

        Web3Controller.SignIn_Demo()
            .OnSuccess(() =>
            {
                SceneManager.LoadScene("main");
            })
            .OnFail(Web3Controller.ShowException)
            .Finally(Web3Controller.HideLoader);
    }

    protected internal void OpenLoginUI()
    {
        EmailInputField.gameObject.SetActive(true);
        PasswordInputField.gameObject.SetActive(true);
        PinInputField.gameObject.SetActive(false);
        NicknameInputField.gameObject.SetActive(false);

        LoginButton.gameObject.SetActive(true);
        DemoButton.gameObject.SetActive(true);
        SignUpButton.gameObject.SetActive(true);

        CreateButton.gameObject.SetActive(false);
        SignUpBackButton.gameObject.SetActive(false);
    }

    protected internal void OpenSignUpUI()
    {
        EmailInputField.gameObject.SetActive(true);
        PasswordInputField.gameObject.SetActive(true);
        PinInputField.gameObject.SetActive(true);
        NicknameInputField.gameObject.SetActive(true);

        LoginButton.gameObject.SetActive(false);
        DemoButton.gameObject.SetActive(false);
        SignUpButton.gameObject.SetActive(false);

        CreateButton.gameObject.SetActive(true);
        SignUpBackButton.gameObject.SetActive(true);
    }
}
