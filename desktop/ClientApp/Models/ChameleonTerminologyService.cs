using System;

namespace ClientApp.Models
{
    public class ChameleonTerms
    {
        public string InstitutionTypeDisplayName { get; set; } = string.Empty;
        public string PersonnelMenuName { get; set; } = string.Empty;
        public string IdentityNumberLabel { get; set; } = string.Empty;
        public string LeaderTitle { get; set; } = string.Empty;
        public string RabFundingSource { get; set; } = string.Empty;
    }

    public static class ChameleonTerminologyService
    {
        public static ChameleonTerms GetTerms(string institutionType)
        {
            return new ChameleonTerms
            {
                InstitutionTypeDisplayName = GetInstitutionBadge(institutionType),
                PersonnelMenuName = GetPersonnelMenuTitle(institutionType),
                IdentityNumberLabel = GetPrimaryIdLabel(institutionType),
                LeaderTitle = GetChiefTitle(institutionType),
                RabFundingSource = GetRabFundingSource(institutionType)
            };
        }

        public static string GetPersonnelMenuTitle(string institutionType) => institutionType switch
        {
            "school" => "Data Guru & Tendik (PTK)",
            "village_gov" => "Data Perangkat & Aparatur",
            "corporate" => "Data Karyawan & Direksi",
            "healthcare" => "Data Tenaga Medis & Pegawai",
            "foundation" => "Data Pengurus & Anggota",
            _ => "Data Pegawai & SDM"
        };

        public static string GetPrimaryIdLabel(string institutionType) => institutionType switch
        {
            "school" => "NIP / NUPTK",
            "village_gov" => "NIPD / NIP / NIK",
            "corporate" => "NIK Karyawan / NPWP",
            "healthcare" => "STR / SIP / NIP",
            "foundation" => "Nomor Anggota / NIK",
            _ => "Nomor Identitas (NIP/NIK)"
        };

        public static string GetChiefTitle(string institutionType) => institutionType switch
        {
            "school" => "Kepala Sekolah",
            "village_gov" => "Kepala Desa / Lurah",
            "corporate" => "Direktur Utama / General Manager",
            "healthcare" => "Direktur RS / Kepala Klinik",
            "foundation" => "Ketua Yayasan",
            _ => "Pimpinan Instansi"
        };

        public static string GetRabFundingSource(string institutionType) => institutionType switch
        {
            "school" => "Dana BOS / Komite Sekolah",
            "village_gov" => "Dana Desa (DD) / APBDes",
            "corporate" => "Anggaran Proyek / Cost Center",
            "healthcare" => "Anggaran Operasional Medis",
            "foundation" => "Dana Kas Yayasan / Donatur",
            _ => "Anggaran Kedinasan"
        };

        public static string GetInstitutionBadge(string institutionType) => institutionType switch
        {
            "school" => "Sekolah / Pendidikan",
            "village_gov" => "Pemerintahan Desa / Daerah",
            "corporate" => "Korporasi / Bisnis Swasta",
            "healthcare" => "Pelayanan Kesehatan / RS",
            "foundation" => "Yayasan / Nirlaba",
            _ => "Instansi Kedinasan"
        };
    }
}
