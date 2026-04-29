using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/boss-mode-config")]
public class BossModeConfig : ControllerBase
{
    [HttpGet]
    public IActionResult GetBossModeConfig()
    {
        return Ok(new
        {
            BossRewardMapping = new Dictionary<string, Dictionary<string, string>>
            {
                // Amanita
                {
                    "8177ecc8-10cd-440b-9988-b39dd4fb5e88", new Dictionary<string, string> {
                        { "Easy", "8177ecc8-10cd-440b-9988-b39dd4fb5e88" },
                        { "Medium", "8177ecc8-10cd-440b-9988-b39dd4fb5e88" },
                        { "Hard", "8177ecc8-10cd-440b-9988-b39dd4fb5e88" }
                    }
                },
                // Cthulhu / Cthylla (also shared by Jinx due to duplicate GUID)
                {
                    "1b4c1baa-074e-48ef-acce-6f305bf9cad4", new Dictionary<string, string> {
                        { "Easy", "1b4c1baa-074e-48ef-acce-6f305bf9cad4" },
                        { "Medium", "1b4c1baa-074e-48ef-acce-6f305bf9cad4" },
                        { "Hard", "1b4c1baa-074e-48ef-acce-6f305bf9cad4" }
                    }
                },
                // Magician / Sally Mystic
                {
                    "332a0149-643b-4aa7-96a5-46a9cbac1303", new Dictionary<string, string> {
                        { "Easy", "332a0149-643b-4aa7-96a5-46a9cbac1303" },
                        { "Medium", "00fe6470-ae39-4f46-af95-101c0170c2f6" },
                        { "Hard", "7c408337-fc69-4235-8fd1-db37e1381970" }
                    }
                },
                // Sarah / Sara
                {
                    "1c886fb3-49d3-446a-bd9d-3d7970277188", new Dictionary<string, string> {
                        { "Easy", "1c886fb3-49d3-446a-bd9d-3d7970277188" },
                        { "Medium", "c7c431a2-4043-4760-b543-66cbce36f94d" },
                        { "Hard", "a17ead1f-1aa4-4153-b007-65624ad9a4dd" }
                    }
                },
                // Simon / Kevin J Simon
                {
                    "a8ac478a-0178-4921-8338-a63f2c60070a", new Dictionary<string, string> {
                        { "Easy", "a8ac478a-0178-4921-8338-a63f2c60070a" },
                        { "Medium", "a8ac478a-0178-4921-8338-a63f2c60070a" },
                        { "Hard", "a8ac478a-0178-4921-8338-a63f2c60070a" }
                    }
                },
            },
        });
    }
}
