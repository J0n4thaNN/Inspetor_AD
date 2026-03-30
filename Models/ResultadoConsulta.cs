using System;

namespace MonitorUsuariosAD.Models
{
    public class ResultadoConsulta
    {
        public string? Alvo { get; set; }
        public string? Usuario { get; set; }
        public string? Status { get; set; }
        public DateTime? LastLogon { get; set; }
    }
}