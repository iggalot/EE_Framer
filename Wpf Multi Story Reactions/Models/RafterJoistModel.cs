namespace StructuralPlanner.Models
{
    public class RafterJoistModel : StructuralMember
    {
        public double Area_DL_psf { get; set; } = 10;   // psf
        public double Area_LL_psf { get; set; } = 20;   // psf


        public double Spacing_in { get; set; } = 16;  // in

        public RafterJoistModel() : base()
        {
            Spacing_in = 16;
            SetDefaultAreaLoads();
        }

        public RafterJoistModel(double spacing) : base()
        {
            Spacing_in = spacing;
            SetDefaultAreaLoads();
        }

        private void SetDefaultAreaLoads()
        {
            if(Type == MemberType.Rafter)
            {
                Area_DL_psf = 10;
                Area_LL_psf = 20;
            } else if (Type == MemberType.FloorJoist)
            {
                Area_DL_psf = 40;
                Area_LL_psf = 10;
            } else if (Type == MemberType.CeilingJoist)
            {
                Area_DL_psf = 10;
                Area_LL_psf = 5;
            } else
            {
                Area_DL_psf = 0;
                Area_LL_psf = 0;
            }
        }


    }
}
