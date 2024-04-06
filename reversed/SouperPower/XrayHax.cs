using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public partial class Soup
{
    public static class XrayHax
    {
        public static RaycastHit? RayhitFirstSolid(Vector3 start, Vector3 end, RaycastHit[] sels = null, RaycastHit[] igns = null) => 
            RayhitFirstSolid(start, (end - start).normalized, Vector3.Distance(start, end), sels, igns);

        public static RaycastHit? RayhitFirstSolid(Vector3 pos, Vector3 dir, float dist, RaycastHit[] sels = null, RaycastHit[] igns = null)
        {
            sels ??= new RaycastHit[0];
            igns ??= new RaycastHit[0];
            
            string[] selsName = new string[sels.Length];
            string[] ignsName = new string[igns.Length];

            for (int i = 0; i < sels.Length; i++)
                selsName[i] = sels[i].transform.gameObject.name;

            for (int i = 0; i < igns.Length; i++)
                ignsName[i] = igns[i].transform.gameObject.name;

            RaycastHit[] hits = Xray(pos, dir, dist);

            if (hits is null) return null;

            foreach (RaycastHit hit in hits)
                if(CandidateMatch(hit, selsName, ignsName)) return hit;

            return null;
        }


        public static RaycastHit[] RayhitAllSolid(Vector3 start, Vector3 end, RaycastHit[] sels = null, RaycastHit[] igns = null) => 
            RayhitAllSolid(start, (end - start).normalized, Vector3.Distance(start, end), sels, igns);

        public static RaycastHit[] RayhitAllSolid(Vector3 pos, Vector3 dir, float dist, RaycastHit[] sels = null, RaycastHit[] igns = null)
        {
            sels ??= new RaycastHit[0];
            igns ??= new RaycastHit[0];
            
            string[] selsName = new string[sels.Length];
            string[] ignsName = new string[igns.Length];

            for (int i = 0; i < sels.Length; i++)
                selsName[i] = sels[i].transform.gameObject.name;

            for (int i = 0; i < igns.Length; i++)
                ignsName[i] = igns[i].transform.gameObject.name;

            RaycastHit[] hits = Xray(pos, dir, dist);

            if (hits is null) return null;

            List<RaycastHit> validhits = new(); 

            foreach (RaycastHit hit in hits)
                if(CandidateMatch(hit, selsName, ignsName)) validhits.Add(hit);

            return validhits.ToArray();
        }

        public static bool CandidateMatch(RaycastHit hit, string[] selsName, string[] ignsName)
        {
            if (hit.collider.isTrigger)
                return false;
            if (hit.transform.GetComponentInParent<Human>())
                return false;
            if (hit.rigidbody)
                return false;
            if (selsName.Length != 0 && !selsName.Contains(hit.transform.gameObject.name))
                return false;
            if (ignsName.Length != 0 && ignsName.Contains(hit.transform.gameObject.name))
                return false;   

            return true;
        }

        public static RaycastHit[] Xray(Vector3 pos, Vector3 dir, float dist) => Physics.RaycastAll(pos, dir, dist, 0b111111111100011111111110101);

    }
}