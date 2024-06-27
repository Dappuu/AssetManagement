import { useNavigate } from "react-router-dom";
import agent from "../../app/api/agent";
import CreateAssetForm from "../../app/components/forms/CreateAssetForm";
import { BaseResult } from "../../app/models/BaseResult";
import { Asset } from "../../app/models/asset/Asset";
import {
  AssetCreationForm,
  AssetCreationRequest,
} from "../../app/models/asset/AssetCreationRequest";

const CreateAssetPage = () => {
  const navigate = useNavigate();

  const handleSubmit = async (data: AssetCreationForm) => {
    const dataRequest: AssetCreationRequest = {
      categoryId: data.categoryId,
      installedDate: data.installedDate,
      name: data.name,
      specification: data.specification,
      state: data.state,
    };

    console.log("Data", data);
    console.log("Data Request", dataRequest);

    const response: BaseResult<Asset> = await agent.Asset.create(dataRequest);
    if (response.isSuccess) {
      navigate(
        `/manage-asset?passedOrderBy=${encodeURIComponent(
          "lastUpdate"
        )}&passedOrder=${encodeURIComponent("desc")}`
      );
    }
  };

  return (
    <div>
      <CreateAssetForm handleCreateAsset={handleSubmit} />
    </div>
  );
};

export default CreateAssetPage;