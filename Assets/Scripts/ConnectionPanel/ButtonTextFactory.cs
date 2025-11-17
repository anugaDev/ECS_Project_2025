using System.Collections.Generic;

public class ButtonTextFactory
{
    private readonly List<string> _buttonTexts;
    
    private string _defaultText;

    public ButtonTextFactory()
    {
        _buttonTexts = new List<string>
        {
            "Start Host",
            "Start Server",
            "Start Client",
        };

        _defaultText = "<ERROR>";
    }
    
    public string GetButtonText(int connectionValue)
    {
        if (_buttonTexts.Count <= connectionValue)
        {
            return _defaultText;
        }

        return _buttonTexts[connectionValue];
    }
}