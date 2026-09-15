using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Type of data merge for the sign template. A seating chart is
    /// an example of a single sign.  A room sign is an example of a
    /// MultiSign where a different sign generated for each
    /// row of data that may have a instructor and room number 
    /// </summary>
    public enum TemplateSignMode
    {
        MultiSign,    // each row of data pasted in represents a record, generating multiple signs
        SingleSign    // each row of data pasted in is a field for a sigle record generating a single sign
    }
}
